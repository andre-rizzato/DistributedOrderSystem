# src/AgentService/rag/retrieval.py
#
# Orchestrates the full retrieval pipeline: HyDE -> embed -> filtered
# Qdrant search -> BM25 -> RRF -> rerank -> top-N. This is the one function
# rag_node.py (the LangGraph node) calls - every other file in rag/ is a
# building block this file composes, never called directly by the graph.
#
# Deterministic-flow principle (see graph.py's existing comments on
# cancel_order_agent_node and route_by_confidence): nothing in this
# pipeline lets the LLM decide WHAT gets returned. HyDE's LLM call produces
# a discardable embedding aid, never shown to the customer and never
# treated as fact. Qdrant filtering, BM25, RRF, and reranking are all
# plain, inspectable code - the only "judgment" happening is Voyage's
# rerank model scoring relevance, which is a retrieval mechanic, not a
# business decision.

from qdrant_client import models

from rag.bm25 import bm25_rank
from rag.embeddings import embed_query
from rag.hybrid import reciprocal_rank_fusion
from rag.hyde import generate_hypothetical_passage
from rag.qdrant_client import get_qdrant_client
from rag.reranker import rerank

# How many candidates come out of the broad (cheap) retrieval stage before
# reranking narrows them down - large enough that a relevant note
# occasionally buried in position 10-15 of pure vector search still has a
# chance to surface after BM25/RRF/rerank, small enough that reranking
# (the expensive, per-document LLM-ish call) stays fast and cheap.
_CANDIDATE_POOL_SIZE = 15
# How many documents the final, reranked result returns to generate_reply_node.
_TOP_N = 3


def _build_qdrant_filter(
    requester_phone: str | None,
    order_status: str | None,
    issue_tag: str | None,
) -> models.Filter | None:
    """
    Builds a Qdrant Filter from whichever fields are present - None stays
    out of the filter entirely (Qdrant has no "match anything" wildcard
    value, so an absent filter condition is the only correct way to express
    "don't filter on this field"). Returns None (no filter at all) if every
    argument is None, e.g. for faq_policy's unfiltered searches.
    """
    conditions = []
    if requester_phone is not None:
        conditions.append(models.FieldCondition(key="customer_phone", match=models.MatchValue(value=requester_phone)))
    if order_status is not None:
        conditions.append(models.FieldCondition(key="order_status", match=models.MatchValue(value=order_status)))
    if issue_tag is not None:
        conditions.append(models.FieldCondition(key="issue_tag", match=models.MatchValue(value=issue_tag)))

    if not conditions:
        return None
    return models.Filter(must=conditions)


def retrieve(
    query: str,
    collection: str,
    requester_phone: str | None = None,
    order_status: str | None = None,
    issue_tag: str | None = None,
    use_hyde: bool = True,
) -> list[dict]:
    """
    Runs the full pipeline against `collection` and returns up to _TOP_N
    results, each {"text": str, "score": float, "payload": dict} - ready
    for generate_reply_node to fold into its grounding context. Returns an
    empty list if the filtered search finds nothing (e.g. no notes for this
    customer_phone/order_status/issue_tag combination) - callers should
    treat that the same way generate_reply_node already treats "order not
    found": say so honestly, never fabricate.
    """
    # HyDE only changes what gets EMBEDDED for the Qdrant vector search -
    # BM25 and the reranker below both use the real `query`, never the
    # hypothetical passage. BM25 matches exact terms, which the hypothetical
    # passage's invented wording would only hurt; the reranker's job is
    # judging real relevance to what the customer actually asked, not to a
    # discardable LLM guess.
    search_text = generate_hypothetical_passage(query) if use_hyde else query
    query_vector = embed_query(search_text)

    qdrant_filter = _build_qdrant_filter(requester_phone, order_status, issue_tag)
    semantic_hits = get_qdrant_client().query_points(
        collection,
        query=query_vector,
        query_filter=qdrant_filter,
        limit=_CANDIDATE_POOL_SIZE,
        with_payload=True,
    ).points

    if not semantic_hits:
        # Nothing matched the filter at all - no candidate pool for
        # BM25/RRF/rerank to work with, so there is nothing further to do.
        return []

    candidate_texts = [hit.payload["note_text"] for hit in semantic_hits]
    candidate_payloads = [hit.payload for hit in semantic_hits]

    # Both rankings below are (score, index) pairs over the SAME
    # candidate_texts list - semantic_ranking's index is just its position
    # in semantic_hits (Qdrant already returns results best-first), bm25
    # computes its own ranking independently over the same pool.
    semantic_ranking = [(hit.score, i) for i, hit in enumerate(semantic_hits)]
    bm25_ranking = bm25_rank(query, candidate_texts)

    fused = reciprocal_rank_fusion([semantic_ranking, bm25_ranking])
    # Re-index everything into the FUSED order - fused_texts[i] and
    # fused_payloads[i] describe the same document, by construction. This
    # is what lets reranker.py's returned index be used directly below,
    # instead of mapping back through text equality (fragile if two
    # candidates ever have identical note_text).
    fused_texts = [candidate_texts[i] for _, i in fused]
    fused_payloads = [candidate_payloads[i] for _, i in fused]

    # Reranking happens over the FUSED order, not the raw candidate_texts
    # order - doesn't change the final result (the reranker scores every
    # document it's given regardless of input order) but keeps the RRF
    # step meaningful to run at all; skipping straight from Qdrant+BM25 to
    # reranking without fusing first would make hybrid.py dead code.
    reranked = rerank(query, fused_texts, top_k=_TOP_N)

    return [
        {"text": fused_texts[index], "score": score, "payload": fused_payloads[index]}
        for score, index in reranked
    ]
