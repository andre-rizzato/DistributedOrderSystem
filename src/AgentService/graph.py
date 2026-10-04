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
    return {
        "final_reply": (
            "I understand you'd like to cancel this order. To make sure this is "
            "handled correctly and securely, I'm connecting you with a human agent "
            "who will confirm the cancellation with you directly."
        )
    }


def create_stub_node(state: AgentState) -> dict:
    print("  [NODE create_stub] (stub - no real GatewayBff endpoint yet)")
    return {"final_reply": "Placing orders through the assistant isn't available yet - please complete your purchase on the site."}


def update_stub_node(state: AgentState) -> dict:
    print("  [NODE update_stub] (stub - no real GatewayBff endpoint yet)")
    return {"final_reply": "Changing orders through the assistant isn't available yet - please contact human support."}


def product_info_stub_node(state: AgentState) -> dict:
    print("  [NODE product_info_stub] (stub - no product search endpoint yet)")
    return {"final_reply": "Looking up products through the assistant isn't available yet - please check the catalog on the site."}


def clarify_node(state: AgentState) -> dict:
    print(f"  [NODE clarify] confidence={state.get('confidence')} <= 0.70 -> asking for clarification")
    return {"final_reply": "I'm not sure I understood - could you rephrase or give more detail about what you need?"}


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
    print(f"  [NODE generate_reply] order_data={'present' if data else 'absent'}, order_number={order_number}")

    if data is not None:
        context = f"Real data for the order looked up in the system: {data}"
    elif order_number:
        context = f"Order number {order_number} was not found in the system."
    else:
        context = "No specific order data available for this question."

    system = (
        "You are the support assistant for DistributedOrderSystem. Reply in English, "
        "briefly and courteously, using ONLY the context provided. If the context says no "
        "data is available, say so clearly instead of making up an answer."
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
        destination = "general_question"
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
            "general_question": "generate_reply",
        },
    )

    graph.add_edge("order_info_agent", "generate_reply")
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
