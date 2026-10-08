# src/AgentService/state.py
# State = the "clipboard" that travels between the graph's nodes (Week 10 of the course).
# Each node only returns the keys it changed - LangGraph merges them into the
# full state on its own, following the signature declared here.

from typing import Optional, TypedDict


class AgentState(TypedDict):
    message: str
    session_id: str

    # Phone number of whoever is asking, when the channel has one to offer
    # (security review 2026-10-04, item #4). Set by main.py from
    # AgentRequest.requester_phone, which the Node orchestrator fills with
    # the WhatsApp sender's wa_id - None for Telegram, since a chat id isn't
    # a verified phone number. Threaded through to order_info_agent_node so
    # a client-specific OrderBackend CAN verify the requester owns the order
    # before returning real data (see connectors/base.py's OrderBackend.get_status
    # docstring) - plumbing only, no generic connector enforces it yet.
    requester_phone: Optional[str]

    # "pt" | "en" | "it", set by main.py from AgentRequest.language - the
    # same value the Node orchestrator's promptBuilder.ts already uses for
    # its own RAG prompt. None on channels that don't inform a language
    # today (WhatsApp) - generate_reply_node falls back to "match the
    # customer's own message" in that case, same as the Node side.
    language: Optional[str]

    # filled in by classify_intent_node (Week 8: few-shot + Pydantic)
    intent: Optional[str]
    order_number: Optional[str]
    confidence: Optional[float]

    # filled in by order_info_agent_node (the only real worker that still
    # reads order_data via generate_reply_node - cancel_order_agent_node no
    # longer calls the backend at all, see its docstring in graph.py)
    order_data: Optional[dict]

    # filled in by generate_reply_node or clarify_node - this is what goes back to C#
    final_reply: Optional[str]

    # filled in by retrieve_knowledge_node (rag_node.py) - never serialized
    # into AgentResponse (main.py), only consumed internally by
    # generate_reply_node to build its grounding context. None means either
    # retrieval was never attempted (order_info/cancel_order/stub intents
    # don't route through retrieve_knowledge_node at all) or it WAS
    # attempted but found nothing / was refused (see rag_node.py's privacy
    # guardrail) - generate_reply_node treats both the same way: ground
    # honestly on "no context available", never fabricate.
    retrieved_context: Optional[list[dict]]
