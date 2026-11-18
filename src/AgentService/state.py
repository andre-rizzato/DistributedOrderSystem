# src/AgentService/state.py
# State = the "clipboard" that travels between the graph's nodes.
# Each node only returns the keys it changed - LangGraph merges them into the
# full state on its own, following the signature declared here.

from typing import Optional, TypedDict


class AgentState(TypedDict):
    message: str
    session_id: str

    # Phone number of whoever is asking, when the channel has one to offer
    # (security review 2026-10-04, item #4). Set by main.py from
    # AgentRequest.requester_phone, which the calling orchestrator fills
    # with the sender's verified phone number when the channel provides
    # one - None for channels whose chat id isn't a verified phone number.
    # Threaded through to order_info_agent_node so
    # a client-specific OrderBackend CAN verify the requester owns the order
    # before returning real data (see connectors/base.py's OrderBackend.get_status
    # docstring) - plumbing only, no generic connector enforces it yet.
    requester_phone: Optional[str]

    # "pt" | "en" | "it", set by main.py from AgentRequest.language - the
    # same value a calling orchestrator may already use for its own RAG
    # prompt. None on channels that don't inform a language -
    # generate_reply_node falls back to "match the customer's own message"
    # in that case.
    language: Optional[str]

    # filled in by classify_intent_node (few-shot + Pydantic)
    intent: Optional[str]
    order_number: Optional[str]
    confidence: Optional[float]

    # filled in by order_info_agent_node (the only real worker that still
    # reads order_data via generate_reply_node - cancel_order_agent_node no
    # longer calls the backend at all, see its docstring in graph.py)
    order_data: Optional[dict]

    # Scheduling capability (calendar integration, 2026-10-11) - filled in
    # by classify_intent_node for the 4 new intents (check_availability,
    # book_appointment, cancel_appointment, reschedule_appointment), read by
    # scheduling_node.py's workers. service_id/requested_date/requested_slot
    # are the LLM's best-effort extraction from the customer's own words
    # ("corte às 15h na sexta") - None when the message didn't mention
    # enough to resolve one deterministically, which is exactly what sends
    # the conversation to clarify_node (route_by_confidence) instead of
    # guessing a slot the customer never said.
    service_id: Optional[str]
    requested_date: Optional[str]
    requested_slot: Optional[str]
    # appointment_id: only ever read by cancel_reschedule_appointment_node
    # for logging - that node never calls backend.cancel() itself (see
    # connectors/base.py's SchedulingBackend.cancel docstring), so this is
    # NOT a trigger for a real cancellation, just the id the customer quoted
    # for the human attendant to find.
    appointment_id: Optional[str]
    # scheduling_result: whatever check_availability_node/book_appointment_node
    # got back from the concrete SchedulingBackend - a list of open slots,
    # a confirmed booking ({"confirmed": True, ...}), or a Calendly
    # scheduling link ({"scheduling_link": url}). generate_reply_node reads
    # this the same way it already reads order_data - raw backend output,
    # formatted into natural language, never fabricated.
    scheduling_result: Optional[dict | list]

    # Sales capability (catalog + checkout, 2026-10-20) - mirrors the
    # scheduling fields above exactly. product_query/quantity are filled in
    # by classify_intent_node for the 2 new intents (sales_check_product,
    # sales_start_checkout); read by sales_node.py's workers. quantity stays
    # a string (not int) because the LLM's best-effort extraction from free
    # text ("quero 2 unidades") is parsed with a fallback-to-1 inside
    # sales_node.py, not here - same "None means not applicable" convention
    # as every other field on this class.
    product_query: Optional[str]
    quantity: Optional[str]
    # sales_result: whatever sales_check_product_node/sales_start_checkout_node
    # got back - a list of matching products (search), or a dict describing
    # either a failure to resolve a single product
    # ({"checkout_failed": "not_found" | "ambiguous" | "invalid_price", ...})
    # or a successful checkout link ({"checkout_url": ..., "reference_id":
    # ..., "reference_code": ...}). generate_reply_node reads this the same
    # way it already reads scheduling_result - raw backend output, formatted
    # into natural language, never fabricated.
    sales_result: Optional[dict | list]

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
