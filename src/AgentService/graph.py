# src/AgentService/graph.py
# Order Orchestrator Agent - the LangGraph graph from Week 10. Skeleton
# agreed with Andre before writing it: Orchestrator (classify_intent)
# + conditional-edge routing (route_by_confidence) + 2 real workers
# (order_info_agent, cancel_order_agent) + 3 stub workers (create/update/product_info),
# all converging on generate_reply or ending directly at END.
#
# Week 10 principle (verification question answered): the routing
# decision lives in a function separate from the node that calls the LLM -
# testable with no network, no cost, no calling the model again.

from anthropic import Anthropic
from langgraph.graph import END, START, StateGraph

from connectors import get_order_backend
from intent_classifier import classify_intent_node
from messages import message
from rag_node import retrieve_knowledge_node
from state import AgentState

anthropic_client = Anthropic()
# Built once at import time, same lifetime as anthropic_client above - the
# concrete class (RestOrderBackend today, pointed at GatewayBff by default)
# is decided by connectors/factory.py reading ORDER_BACKEND /
# ORDER_API_BASE_URL, not by anything in this file. Swapping which client
# system this graph talks to is a .env change, never a code change here.
order_backend = get_order_backend()


def order_info_agent_node(state: AgentState) -> dict:
    order_number = state.get("order_number")
    # requester_phone threaded through for whichever OrderBackend is wired
    # in to optionally verify the requester owns this order before handing
    # back real data (security review 2026-10-04, item #4) - see
    # connectors/base.py's OrderBackend.get_status docstring for why no
    # generic connector enforces this today.
    requester_phone = state.get("requester_phone")
    data = order_backend.get_status(order_number, requester_phone=requester_phone) if order_number else None
    print(f"  [NODE order_info_agent] order_number={order_number} -> order_data={data}")
    return {"order_data": data}


def cancel_order_agent_node(state: AgentState) -> dict:
    # Security review 2026-10-04 (docs/SECURITY_REVIEW.md item #4): a
    # cancellation must ALWAYS become a human handoff - the agent never
    # calls order_backend.cancel() itself, no matter how confident the
    # intent classification is. Two reasons: canceling is destructive and
    # hard to undo for the customer, and even the requester_phone check
    # order_info_agent_node above is starting to grow (see
    # connectors/base.py) wouldn't be enough by itself here - a destructive
    # action deserves a human in the loop regardless of how confident the
    # identity check is.
    # Deliberately ends the graph here (see build_graph: this node routes
    # straight to END, not through generate_reply_node) instead of calling
    # the LLM - there is nothing for a model to decide, the outcome is
    # always the same message, so skipping the model call is both cheaper
    # and more predictable.
    order_number = state.get("order_number")
    print(f"  [NODE cancel_order_agent] order_number={order_number} -> human handoff (agent never cancels directly)")
    return {"final_reply": message("cancel_order_handoff", state.get("language"))}


def create_stub_node(state: AgentState) -> dict:
    print("  [NODE create_stub] (stub - no real GatewayBff endpoint yet)")
    return {"final_reply": message("create_stub", state.get("language"))}


def update_stub_node(state: AgentState) -> dict:
    print("  [NODE update_stub] (stub - no real GatewayBff endpoint yet)")
    return {"final_reply": message("update_stub", state.get("language"))}


def product_info_stub_node(state: AgentState) -> dict:
    print("  [NODE product_info_stub] (stub - no product search endpoint yet)")
    return {"final_reply": message("product_info_stub", state.get("language"))}


def clarify_node(state: AgentState) -> dict:
    print(f"  [NODE clarify] confidence={state.get('confidence')} <= 0.70 -> asking for clarification")
    return {"final_reply": message("clarify", state.get("language"))}


def generate_reply_node(state: AgentState) -> dict:
    # Grounding (same principle as Weeks 3-4): the LLM only formats what's
    # already in the state, it never makes up a policy or data it wasn't given.
    #
    # No cancel_result branch here anymore (security review 2026-10-04,
    # item #4): cancel_order_agent_node now ends the graph directly (see
    # build_graph's "cancel_order_agent" -> END edge) instead of routing
    # here, since it never calls the backend and always returns the same
    # fixed handoff message - there's nothing left for this node to decide
    # about a cancellation. For the same reason, intent=="cancel_order"
    # never reaches this node: route_by_confidence only sends it to
    # cancel_order_agent (order_number set) or clarify (order_number
    # missing), never here.
    data = state.get("order_data")
    order_number = state.get("order_number")
    # retrieved_context: filled in by retrieve_knowledge_node (rag_node.py)
    # for general_question/order_history_query intents - None either means
    # that node never ran (order_info/cancel_order/stub intents don't route
    # through it) or it ran and found/was allowed to return nothing (see
    # rag_node.py's privacy guardrail). Checked AFTER order_data on purpose:
    # order_info_agent_node's single-record lookup takes priority when both
    # happen to be present, since it's the more specific, already-verified
    # data source for that intent.
    retrieved = state.get("retrieved_context")
    print(
        f"  [NODE generate_reply] order_data={'present' if data else 'absent'}, "
        f"order_number={order_number}, retrieved_context={len(retrieved) if retrieved else 0} chunk(s)"
    )

    if data is not None:
        context = f"Real data for the order looked up in the system: {data}"
    elif retrieved:
        # Joined as a bulleted list, same augmentation shape as the
        # course's week3-4 RAG-from-scratch exercise - each chunk's text
        # only, not its score/payload (those are retrieval-internal, not
        # something the LLM needs to see to answer).
        context = "Relevant information found:\n" + "\n".join(f"- {chunk['text']}" for chunk in retrieved)
    elif retrieved is not None and state.get("intent") == "order_history_query":
        # retrieved == [] specifically (not None) means retrieval WAS
        # attempted (identity was verified) but found nothing matching -
        # different from the "identity not verified" case below, worth
        # distinguishing in case this ever needs separate handling, even
        # though today both branches lead to a similar honest non-answer.
        context = "No matching order history was found for this question."
    elif state.get("intent") == "order_history_query":
        # retrieved is None AND the intent needed identity verification -
        # this is rag_node.py's privacy guardrail firing (no
        # requester_phone on this channel). Telling the customer the truth
        # here, not a generic failure message - same "ground honestly,
        # never fabricate" principle as every other branch in this function.
        context = "I can't look up your order history on this channel without verifying your identity first."
    elif order_number:
        context = f"Order number {order_number} was not found in the system."
    else:
        context = "No specific order data available for this question."

    # Instrução de idioma: mesmo raciocínio do Node (promptBuilder.ts,
    # bug corrigido lá em 06/10/2026) - sem isso, esta função respondia
    # SEMPRE em inglês (valor fixo que ficava aqui antes), mesmo pra
    # cliente que escreveu em português ou italiano. `state["language"]`
    # vem do canal via Node orchestrator (agentServiceClient.ts ->
    # AgentRequest.language, main.py); None quando o canal não informa
    # nenhum (hoje: WhatsApp) - nesse caso, segue o idioma da própria
    # mensagem do cliente, igual ao fallback do lado Node.
    language = state.get("language")
    language_names = {"pt": "Portuguese", "en": "English", "it": "Italian"}
    language_instruction = (
        f"Reply in {language_names.get(language, language)}, even if the context below is in "
        "a different language (translate the information from the context)."
        if language
        else "Reply in the same language the customer used in their message, even if the "
        "context below is in a different language (translate the information from the context)."
    )
    system = (
        "You are the support assistant for DistributedOrderSystem. "
        f"{language_instruction} Reply "
        "briefly and courteously, using ONLY the context provided. If the context says no "
        "data is available, say so clearly instead of making up an answer. Do not add "
        "suggestions, recommendations, or next steps that are not themselves stated in the "
        "context - e.g. if the context says a card was declined, don't add \"you may want to "
        "contact your bank\" unless the context itself says to. Every sentence in your reply "
        "should be traceable to something in the context, not generic customer-service advice."
    )
    response = anthropic_client.messages.create(
        model="claude-sonnet-4-6",
        max_tokens=300,
        system=system,
        messages=[{
            "role": "user",
            "content": f"Context:\n{context}\n\nCustomer question: {state['message']}",
        }],
    )
    return {"final_reply": response.content[0].text}


def route_by_confidence(state: AgentState) -> str:
    # Pure function, no model call - testable with a fake dict in
    # microseconds (Week 10's verification question).
    confidence = state.get("confidence")
    intent = state.get("intent")

    if confidence is None or confidence <= 0.70:
        destination = "clarify"
    elif intent == "cancel_order":
        destination = "cancel_order_agent" if state.get("order_number") else "clarify"
    elif intent == "create_order":
        destination = "create_stub"
    elif intent == "update_order":
        destination = "update_stub"
    elif intent == "get_info":
        destination = "order_info" if state.get("order_number") else "product_info_stub"
    elif intent == "general_question":
        # Used to go straight to "generate_reply" with zero context (see
        # this file's git history before the Order History RAG Agent) -
        # now routes through retrieve_knowledge first, which queries the
        # public faq_policy collection for this intent (see rag_node.py).
        destination = "retrieve_knowledge"
    elif intent == "order_history_query":
        # Same destination node as general_question above - rag_node.py's
        # retrieve_knowledge_node branches internally on `intent` to pick
        # which Qdrant collection to query (and, for this intent only,
        # whether requester_phone is present before querying at all). One
        # node, not two, because the only difference between the two paths
        # is WHICH corpus and WHETHER a privacy check applies - not the
        # shape of the work being done.
        destination = "retrieve_knowledge"
    else:
        destination = "clarify"  # an unexpected label should never reach here - safety guard

    print(f"  [ROUTING] intent={intent} confidence={confidence} -> destination={destination}")
    return destination


def build_graph():
    graph = StateGraph(AgentState)

    graph.add_node("classify_intent", classify_intent_node)
    graph.add_node("order_info_agent", order_info_agent_node)
    graph.add_node("cancel_order_agent", cancel_order_agent_node)
    graph.add_node("create_stub", create_stub_node)
    graph.add_node("update_stub", update_stub_node)
    graph.add_node("product_info_stub", product_info_stub_node)
    graph.add_node("clarify", clarify_node)
    graph.add_node("generate_reply", generate_reply_node)
    graph.add_node("retrieve_knowledge", retrieve_knowledge_node)

    graph.add_edge(START, "classify_intent")

    graph.add_conditional_edges(
        "classify_intent",
        route_by_confidence,
        {
            "clarify": "clarify",
            "order_info": "order_info_agent",
            "cancel_order_agent": "cancel_order_agent",
            "create_stub": "create_stub",
            "update_stub": "update_stub",
            "product_info_stub": "product_info_stub",
            # Both general_question and order_history_query land here -
            # see route_by_confidence's comments on why one node covers
            # both.
            "retrieve_knowledge": "retrieve_knowledge",
        },
    )

    graph.add_edge("order_info_agent", "generate_reply")
    # retrieve_knowledge always flows into generate_reply next, whether it
    # found 3 chunks, 0 chunks, or skipped retrieval entirely (flag off /
    # privacy guardrail) - generate_reply_node's branches on
    # retrieved_context (see its comments above) are what turn each of
    # those outcomes into an honest reply.
    graph.add_edge("retrieve_knowledge", "generate_reply")
    # cancel_order_agent routes straight to END, not generate_reply (security
    # review 2026-10-04, item #4) - it never calls the model, since the
    # outcome (human handoff) never depends on any data generate_reply_node
    # would otherwise format.
    graph.add_edge("cancel_order_agent", END)
    graph.add_edge("generate_reply", END)
    graph.add_edge("clarify", END)
    graph.add_edge("create_stub", END)
    graph.add_edge("update_stub", END)
    graph.add_edge("product_info_stub", END)

    return graph.compile()
