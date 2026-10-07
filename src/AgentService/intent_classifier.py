# src/AgentService/intent_classifier.py
# Same pattern as Week 8 (ai/week8/intent_classification.py): few-shot +
# structured output via Instructor/Pydantic, response_model trades "give me
# text" for "give me a validated IntentClassification instance". The Literal
# guarantees only one of the 5 labels ever comes out of here, never a made-up label.
#
# Graph node: receives the AgentState, returns only the keys it fills in
# (intent, order_number, confidence) - it doesn't decide routing, that's
# the job of the conditional edge function in graph.py.

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
        "(general_question)?"
    ))

    order_number: Optional[str] = Field(
        default=None,
        description="Order number mentioned in the message, if any (e.g. '12345'). None if there isn't one.",
    )

    confidence: float = Field(
        ge=0.0, le=1.0,
        description="Model confidence in the classification, from 0.0 to 1.0.",
    )


# Same examples as Week 8 - one per label, response already in the expected
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
]


def classify_intent_node(state: AgentState) -> dict:
    print(f"  [NODE classify_intent] message received: {state['message']!r}")
    messages = []
    for question, answer in FEW_SHOT_EXAMPLES:
        messages.append({"role": "user", "content": question})
        messages.append({"role": "assistant", "content": answer.model_dump_json()})
    messages.append({"role": "user", "content": state["message"]})

    result: IntentClassification = client.messages.create(
        model="claude-sonnet-4-6",
        max_tokens=1024,
        response_model=IntentClassification,
        messages=messages,
    )

    print(
        f"  [NODE classify_intent] intent={result.intent} "
        f"order_number={result.order_number} confidence={result.confidence:.2f}"
    )

    return {
        "intent": result.intent,
        "order_number": result.order_number,
        "confidence": result.confidence,
    }
