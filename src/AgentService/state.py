# src/AgentService/state.py
# State = the "clipboard" that travels between the graph's nodes.
# Each node only returns the keys it changed - LangGraph merges them into the
# full state on its own, following the signature declared here.

from typing import Optional, TypedDict


class AgentState(TypedDict):
    message: str
    session_id: str

    # filled in by classify_intent_node (few-shot + Pydantic)
    intent: Optional[str]
    order_number: Optional[str]
    confidence: Optional[float]

    # filled in by whichever worker ran (today only order_info_agent_node is real)
    order_data: Optional[dict]

    # filled in by generate_reply_node or clarify_node - this is what goes back to C#
    final_reply: Optional[str]
