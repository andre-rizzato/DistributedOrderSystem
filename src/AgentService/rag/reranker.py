# src/AgentService/rag/reranker.py
#
# Thin wrapper over Voyage's hosted Rerank API - port of CursoClaude's
# week12/reranking.py. A reranker is a CROSS-encoder: unlike the Voyage
# embedding model (a bi-encoder, embeddings.py), which embeds the query and
# each document SEPARATELY and compares the two fixed vectors afterward, a
# cross-encoder reads the query and a document TOGETHER in one pass and
# outputs a single relevance score for that pair. More expensive per
# document (nothing can be precomputed - the document's representation
# only exists once the query is known), but more accurate, because the
# model actually sees how the two texts interact rather than comparing two
# vectors that were never in the same room.
#
# Because of that cost, reranking only ever runs over a SMALL candidate set
# (the output of hybrid.py's RRF, not the whole collection) - this mirrors
# the real production pipeline: cheap broad retrieval first (Qdrant +
# BM25 + RRF), expensive precise reranking only on the finalists.
#
# No local cross-encoder model is used here (e.g. a HuggingFace
# sentence-transformers cross-encoder) - that would mean downloading and
# running a model locally, a meaningfully heavier dependency than the
# hosted Voyage API call, which matters on the 892MB shared deploy VM (see
# rag/config.py's top comment).

import os

import voyageai

from rag.config import VOYAGE_RERANK_MODEL

# Separate client instance from embeddings.py's voyage_client - cheap to
# construct, and keeps this module's dependency on embeddings.py at zero
# (no reason for reranking to care how embedding is implemented, or
# vice-versa).
_voyage_client = voyageai.Client(api_key=os.getenv("VOYAGE_API_KEY"))


def rerank(query: str, docs: list[str], top_k: int | None = None) -> list[tuple[float, int]]:
    """
    Reranks `docs` against `query` using Voyage's rerank-2 model. Returns
    (relevance_score, original_index) pairs, already sorted best-first by
    the API itself - no local re-sorting needed, unlike bm25_rank()/
    reciprocal_rank_fusion() above, which have to sort themselves.
    `top_k=None` returns every document reranked; passing a number truncates
    server-side instead of over the network then discarding locally.
    """
    result = _voyage_client.rerank(query=query, documents=docs, model=VOYAGE_RERANK_MODEL, top_k=top_k)
    # .index is the position of this document in the ORIGINAL `docs` list
    # passed in - not a new id Voyage invented - same reasoning as
    # bm25_rank() returning indices instead of text: avoids any ambiguity
    # if two candidate documents happen to have identical or near-identical
    # text.
    return [(r.relevance_score, r.index) for r in result.results]
