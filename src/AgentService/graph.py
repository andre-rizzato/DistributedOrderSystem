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

from gateway_client import cancel_order_by_id, get_order_by_id
from intent_classifier import classify_intent_node
from state import AgentState

anthropic_client = Anthropic()


def order_info_agent_node(state: AgentState) -> dict:
    order_number = state.get("order_number")
    data = get_order_by_id(order_number) if order_number else None
    print(f"  [NODE order_info_agent] order_number={order_number} -> order_data={data}")
    return {"order_data": data}


def cancel_order_agent_node(state: AgentState) -> dict:
    # Real worker, same shape as order_info_agent_node: routing already
    # guarantees order_number is set before this node runs.
    order_number = state.get("order_number")
    result = cancel_order_by_id(order_number) if order_number else None
    print(f"  [NODE cancel_order_agent] order_number={order_number} -> cancel_result={result}")
    return {"cancel_result": result}


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
    data = state.get("order_data")
    cancel_result = state.get("cancel_result")
    order_number = state.get("order_number")
    print(
        f"  [NODE generate_reply] order_data={'present' if data else 'absent'}, "
        f"cancel_result={'present' if cancel_result else 'absent'}, order_number={order_number}"
    )

    if cancel_result is not None:
        if cancel_result.get("isCanceled"):
            context = f"Order {order_number} was successfully canceled: {cancel_result}"
        else:
            context = (
                f"Order {order_number} could NOT be canceled (e.g. it may already be "
                f"shipped, delivered, or already canceled): {cancel_result}"
            )
    elif data is not None:
        context = f"Real data for the order looked up in the system: {data}"
    elif order_number and state.get("intent") == "cancel_order":
        context = f"Order number {order_number} was not found in the system, so it could not be canceled."
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
    graph.add_edge("cancel_order_agent", "generate_reply")
    graph.add_edge("generate_reply", END)
    graph.add_edge("clarify", END)
    graph.add_edge("create_stub", END)
    graph.add_edge("update_stub", END)
    graph.add_edge("product_info_stub", END)

    return graph.compile()
