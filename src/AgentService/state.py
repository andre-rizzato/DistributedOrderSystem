# src/AgentService/state.py
# State = the "clipboard" that travels between the graph's nodes (Week 10 of the course).
# Each node only returns the keys it changed - LangGraph merges them into the
# full state on its own, following the signature declared here.

from typing import Optional, TypedDict


class AgentState(TypedDict):
    message: str
    session_id: str

    # filled in by classify_intent_node (Week 8: few-shot + Pydantic)
    intent: Optional[str]
    order_number: Optional[str]
    confidence: Optional[float]

    # filled in by whichever worker ran (order_info_agent_node and cancel_order_agent_node are real)
    order_data: Optional[dict]
    cancel_result: Optional[dict]

    # filled in by generate_reply_node or clarify_node - this is what goes back to C#
    final_reply: Optional[str]
