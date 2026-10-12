# src/AgentService/intent_classifier.py
# Pattern: few-shot +
# structured output via Instructor/Pydantic, response_model trades "give me
# text" for "give me a validated IntentClassification instance". The Literal
# guarantees only one of the 10 labels ever comes out of here, never a made-up label.
#
# Graph node: receives the AgentState, returns only the keys it fills in
# (intent, order_number, service_id, requested_date, requested_slot,
# confidence) - it doesn't decide routing, that's the job of the
# conditional edge function in graph.py.

import os
from typing import Literal, Optional

import instructor
from anthropic import Anthropic
from pydantic import BaseModel, Field

from state import AgentState

client = instructor.from_anthropic(Anthropic())


class IntentClassification(BaseModel):
    """Intent classification for a message about the orders domain."""

    intent: Literal[
        "create_order",
        "cancel_order",
        "update_order",
        "get_info",
        "general_question",
        # Order History RAG Agent (rag_node.py): distinct from
        # general_question specifically because it requires a verified
        # requester_phone before any retrieval happens (a customer's own
        # support history is private; FAQ/policy content under
        # general_question is public) - see the implementation plan's
        # section 1.4 and rag_node.py's privacy guardrail for the full
        # reasoning on why this needed to be its own label instead of a
        # branch inside general_question.
        "order_history_query",
        # Scheduling capability (calendar integration, 2026-10-11) - a
        # SEPARATE domain from the 6 labels above (appointments, not
        # orders), but classified by the same node/model: one LLM call per
        # message is cheaper than running two classifiers, and the Node
        # orchestrator's capabilityRouter.ts already guarantees this graph
        # is only reached for a client that actually enabled "scheduling".
        # check_availability: "what times are open" - no destructive
        # action, always safe to answer directly. book_appointment: the
        # customer wants to take one of those times. cancel_appointment /
        # reschedule_appointment: ALWAYS routed to human handoff by
        # route_by_confidence + scheduling_node.py's
        # cancel_reschedule_appointment_node, which never calls a
        # SchedulingBackend - same policy as cancel_order above, for the
        # same reason (destructive, no reliable identity check on every
        # channel). Kept as two distinct labels (not folded together) so the
        # handoff message can tell the customer which one it understood,
        # even though both end up at the same node.
        "check_availability",
        "book_appointment",
        "cancel_appointment",
        "reschedule_appointment",
    ] = Field(description=(
        "The single label that best describes what the user wants. "
        "IMPORTANT distinctions between labels that can all appear with no order number: "
        "get_info (no number) is ONLY for questions about the PRODUCT CATALOG (e.g. 'do you have product Y in stock?'). "
        "order_history_query is for questions about something that ACTUALLY HAPPENED on the customer's OWN "
        "account - status, delays, payment problems, damaged items, refunds already requested, 'why IS my order "
        "late', etc. general_question covers POLICY/HYPOTHETICAL questions even when phrased in first person or "
        "about 'an order' - 'IF I cancel an order, is there a fee', 'what's the refund window', 'can I get a "
        "refund for an order cancelled a month after delivery' (asking what the RULE is, not reporting something "
        "that happened to them) all stay general_question. The test: does answering require looking up THIS "
        "customer's actual records (order_history_query), or just stating a policy that applies to everyone "
        "(general_question)? For the scheduling labels: check_availability is 'what times are open' with no "
        "commitment yet ('do you have anything Friday?'). book_appointment is the customer accepting or naming a "
        "specific time to take ('I'll take 3pm', 'can I book Friday at 3?'). cancel_appointment / "
        "reschedule_appointment are about an appointment that ALREADY EXISTS - canceling it outright vs. moving it "
        "to a different time; use reschedule_appointment only when the customer explicitly wants to move an "
        "existing booking, not when they're simply picking a time for a new one (that's book_appointment)."
    ))

    order_number: Optional[str] = Field(
        default=None,
        description="Order number mentioned in the message, if any (e.g. '12345'). None if there isn't one.",
    )

    # Scheduling capability (calendar integration, 2026-10-11) - populated
    # only for the 4 scheduling intents above; stay None for every
    # order-domain label, same "None means not applicable to this intent"
    # convention order_number already follows for non-order intents.
    service_id: Optional[str] = Field(
        default=None,
        description=(
            "Which configured service the customer means (e.g. 'corte', 'coloracao'), ONLY when the message maps "
            "clearly onto one of the service ids listed in the prompt's 'Available services' hint, if present. "
            "None if no hint was given, or the message doesn't clearly name one of the listed services."
        ),
    )
    requested_date: Optional[str] = Field(
        default=None,
        description=(
            "The calendar date (YYYY-MM-DD) the customer is asking about or wants to book, if a specific day was "
            "mentioned or can be unambiguously resolved from relative phrasing ('tomorrow', 'next Friday') given "
            "the message timestamp. None if no date was mentioned or it stayed ambiguous - ambiguous dates belong "
            "in clarify_node (route_by_confidence), not a guess here."
        ),
    )
    requested_slot: Optional[str] = Field(
        default=None,
        description=(
            "The exact start time the customer wants to book, as an ISO 8601 datetime (YYYY-MM-DDTHH:MM:SS), only "
            "when both a date and a specific time were given or clearly implied ('Friday at 3pm'). None for "
            "check_availability (no commitment yet) or when the time wasn't specific enough to resolve."
        ),
    )

    confidence: float = Field(
        ge=0.0, le=1.0,
        description="Model confidence in the classification, from 0.0 to 1.0.",
    )


# Few-shot examples - one per label, response already in the expected
# JSON format, reinforcing that the OUTPUT follows the schema, not just the input.
FEW_SHOT_EXAMPLES = [
    ("I want to place a new order for 2 units of product X",
     IntentClassification(intent="create_order", order_number=None, confidence=0.97)),
    ("Please cancel order 12346",
     IntentClassification(intent="cancel_order", order_number="12346", confidence=0.98)),
    ("I need to change the delivery address for order 12350",
     IntentClassification(intent="update_order", order_number="12350", confidence=0.95)),
    ("What's the status of my order 12345?",
     IntentClassification(intent="get_info", order_number="12345", confidence=0.96)),
    ("Do you have product Y in stock?",
     IntentClassification(intent="get_info", order_number=None, confidence=0.9)),
    ("What's your refund policy?",
     IntentClassification(intent="general_question", order_number=None, confidence=0.9)),
    ("Did I have any payment problems with my past orders?",
     IntentClassification(intent="order_history_query", order_number=None, confidence=0.93)),
    # Distinguishes this from "Do you have product Y in stock?" above - both
    # are get_info-shaped questions with no order number, but this one is
    # about the customer's OWN order (needs order_support_notes + identity),
    # not the product catalog. Without this example, a question like this
    # was observed to get classified as get_info -> product_info_stub (a
    # "not available yet" stub reply), which is wrong on two counts: it's
    # not a product question, and a real answer was available all along.
    ("Why is my order taking so long to arrive?",
     IntentClassification(intent="order_history_query", order_number=None, confidence=0.9)),
    # Contrast case for the example above: first-person phrasing ("an
    # order I cancelled") that's actually asking about the POLICY/RULE,
    # not reporting a real event on this customer's account - stays
    # general_question. Without this example, a question shaped like this
    # was observed to get misclassified as order_history_query, which
    # wrongly triggers the identity-verification guardrail (rag_node.py)
    # for a question that never needed to look up any real order.
    ("Can I get a refund for an order I cancelled over a month after delivery?",
     IntentClassification(intent="general_question", order_number=None, confidence=0.85)),
    # Scheduling capability (calendar integration, 2026-10-11) - one example
    # per label, same format as the order-domain examples above. The
    # service_id "corte" here is deliberately one of the ids SCHEDULING_SERVICES_HINT
    # (below) would list for a barbershop client - these few-shot examples
    # teach the SHAPE of the extraction, not a universal vocabulary; a real
    # client's own service ids come from their own SCHEDULING_SERVICES env
    # var and are appended to the prompt at call time, see classify_intent_node.
    ("Do you have anything open for a haircut this Friday?",
     IntentClassification(
         intent="check_availability", order_number=None, service_id="corte",
         requested_date=None, requested_slot=None, confidence=0.93,
     )),
    ("I'll take the 3pm slot on Friday for a haircut",
     IntentClassification(
         intent="book_appointment", order_number=None, service_id="corte",
         requested_date=None, requested_slot=None, confidence=0.95,
     )),
    ("I need to cancel my appointment for tomorrow",
     IntentClassification(
         intent="cancel_appointment", order_number=None, service_id=None,
         requested_date=None, requested_slot=None, confidence=0.94,
     )),
    # Contrast case: moving an EXISTING booking, not picking a time for a
    # new one - without this example, "reagendar" phrasing was the kind of
    # thing that risks collapsing into book_appointment, which would skip
    # the mandatory human handoff this label exists to trigger.
    ("Can we move my appointment to next Monday instead?",
     IntentClassification(
         intent="reschedule_appointment", order_number=None, service_id=None,
         requested_date=None, requested_slot=None, confidence=0.9,
     )),
]


# Preâmbulo: build_scheduling_services_hint() lê SCHEDULING_SERVICES direto
# do ambiente (mesmo formato "corte:30,coloracao:90" que
# connectors/factory.py já parseia pro SchedulingBackend) só pra extrair os
# IDS - NUNCA constrói um SchedulingBackend aqui, de propósito: diferente de
# order_backend (sempre presente, construído uma vez no import de graph.py),
# "scheduling" é uma capacidade opcional por cliente, e exigir
# GOOGLE_SERVICE_ACCOUNT_FILE/credenciais de calendário só pra montar um
# classificador de intenção derrubaria o boot de QUALQUER cliente que não
# usa agendamento. Sem a variável definida (o caso comum), devolve None e o
# prompt simplesmente não menciona serviços - o modelo então não tenta
# preencher service_id (vai ficar None), o que é o comportamento correto.
def build_scheduling_services_hint() -> Optional[str]:
    raw = os.getenv("SCHEDULING_SERVICES")
    if not raw:
        return None
    service_ids = [entry.split(":")[0].strip() for entry in raw.split(",") if entry.strip()]
    return ", ".join(service_ids) if service_ids else None


def classify_intent_node(state: AgentState) -> dict:
    print(f"  [NODE classify_intent] message received: {state['message']!r}")
    messages = []
    for question, answer in FEW_SHOT_EXAMPLES:
        messages.append({"role": "user", "content": question})
        messages.append({"role": "assistant", "content": answer.model_dump_json()})

    # Dica de serviços (ver build_scheduling_services_hint acima) entra só
    # na mensagem REAL do cliente, nunca nos few-shot examples - são esses
    # ids que de fato existem pro cliente configurado nesta VM, e é contra
    # eles que service_id precisa bater exatamente (connectors/factory.py's
    # SCHEDULING_SERVICES dict é indexado por esse id).
    services_hint = build_scheduling_services_hint()
    user_content = state["message"]
    if services_hint:
        user_content = f"{user_content}\n\n(Available services: {services_hint})"
    messages.append({"role": "user", "content": user_content})

    result: IntentClassification = client.messages.create(
        model="claude-sonnet-4-6",
        max_tokens=1024,
        response_model=IntentClassification,
        messages=messages,
    )

    print(
        f"  [NODE classify_intent] intent={result.intent} "
        f"order_number={result.order_number} confidence={result.confidence:.2f} "
        f"service_id={result.service_id} requested_date={result.requested_date} "
        f"requested_slot={result.requested_slot}"
    )

    return {
        "intent": result.intent,
        "order_number": result.order_number,
        "service_id": result.service_id,
        "requested_date": result.requested_date,
        "requested_slot": result.requested_slot,
        "confidence": result.confidence,
    }
