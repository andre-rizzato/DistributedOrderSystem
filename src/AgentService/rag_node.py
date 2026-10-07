# src/AgentService/rag_node.py
#
# The Order History RAG Agent node - the graph node that was mapped in the
# project's architecture docs since Week 10 but never actually built until
# now. Sits between classify_intent/route_by_confidence and
# generate_reply_node, the same slot "general_question" already routed
# through with zero context (see graph.py's routing table before this
# change) - this node is what finally fills that gap.

from rag.config import COLLECTION_FAQ_POLICY, COLLECTION_ORDER_SUPPORT_NOTES, ORDER_HISTORY_RAG_ENABLED
from rag.filters import extract_retrieval_filters
from rag.retrieval import retrieve
from state import AgentState


def retrieve_knowledge_node(state: AgentState) -> dict:
    intent = state.get("intent")
    message = state["message"]
    requester_phone = state.get("requester_phone")

    print(f"  [NODE retrieve_knowledge] intent={intent} requester_phone={'present' if requester_phone else 'absent'}")

    if not ORDER_HISTORY_RAG_ENABLED:
        # Production kill switch (see rag/config.py's top comment for why
        # this exists) - with the flag off, this node returns exactly what
        # it returned before it existed: no context, letting
        # generate_reply_node fall back to its original "no specific order
        # data available" branch. The graph's shape doesn't change, only
        # this one node's behavior does - nothing downstream needs to know
        # the flag exists.
        print("  [NODE retrieve_knowledge] ORDER_HISTORY_RAG_ENABLED=false -> skipping retrieval")
        return {"retrieved_context": None}

    if intent == "order_history_query":
        # Privacy guardrail - deliberate design decision, not an
        # afterthought (see the implementation plan's section 1.4).
        # requester_phone is always None on channels with no verified
        # identity (Telegram, today - see connectors/base.py's docstring on
        # OrderBackend.get_status for the matching discussion on the
        # order_info_agent_node side). Without it, this node must not even
        # ATTEMPT a query against order_support_notes - a customer's
        # support history is private, and "no filter" would mean "every
        # customer's notes are candidates" (confirmed empirically while
        # building this: running retrieve() with no requester_phone filter
        # on this same collection returns other customers' notes - the
        # risk is real, not theoretical).
        if not requester_phone:
            print("  [NODE retrieve_knowledge] order_history_query with no requester_phone -> refusing to query, identity not verified")
            return {"retrieved_context": None}

        filters = extract_retrieval_filters(message)
        print(f"  [NODE retrieve_knowledge] extracted filters: order_status={filters.order_status} issue_tag={filters.issue_tag}")

        results = retrieve(
            query=message,
            collection=COLLECTION_ORDER_SUPPORT_NOTES,
            requester_phone=requester_phone,
            order_status=filters.order_status,
            issue_tag=filters.issue_tag,
        )
    else:
        # general_question (or anything else routed here) - public
        # FAQ/policy corpus, no identity check needed, no structured
        # filter either (faq_policy has no per-customer data to scope by).
        results = retrieve(query=message, collection=COLLECTION_FAQ_POLICY)

    print(f"  [NODE retrieve_knowledge] retrieved {len(results)} chunk(s)")
    return {"retrieved_context": results}
