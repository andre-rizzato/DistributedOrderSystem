# src/AgentService/rag/filters.py
#
# Extracts STRUCTURED filters from the customer's free-text message, via
# Instructor + Pydantic - same pattern as intent_classifier.py, not a
# second freeform LLM judgment call. The output is a validated instance of
# a Literal-constrained model: the LLM can only ever return one of the
# known order_status/issue_tag values (or None), never an invented one -
# the schema enforces that, not the model "behaving well".
#
# Why this needs its own LLM call instead of being folded into
# classify_intent_node: intent classification answers "what does the
# customer want" (a routing decision); this answers "what, if any,
# structured facts does their message imply" (a retrieval-filtering
# decision) - two different jobs, and keeping them separate keeps each
# Pydantic model small and each call's few-shot examples focused.

from typing import Literal, Optional

import instructor
from anthropic import Anthropic
from pydantic import BaseModel, Field

# Separate client instance from intent_classifier.py's - same reasoning as
# hyde.py's standalone client: no cross-dependency between rag/ files and
# the files outside it, so rag/ stays a self-contained module.
_client = instructor.from_anthropic(Anthropic())


class RetrievalFilters(BaseModel):
    """Structured facts extracted from a customer message, used to filter the order_support_notes Qdrant collection."""

    order_status: Optional[Literal["Pending", "Confirmed", "Shipped", "Delivered", "Cancelled"]] = Field(
        default=None,
        description=(
            "The order's CURRENT status, set ONLY when the customer's message states or directly "
            "confirms it (e.g. they say 'cancelled', 'still pending', 'it got delivered', 'has it shipped yet'). "
            "Do NOT infer a status from an indirect description of what happened - e.g. 'my item arrived "
            "broken' does NOT mean order_status=Delivered: the item physically arriving doesn't tell you "
            "the order's current status in our system (it could have been cancelled afterward, refunded, "
            "etc.) - leave this None whenever you're inferring rather than reading an explicit statement. "
            "A wrong guess here silently filters out the real matching record, so when in doubt, leave it None."
        ),
    )
    issue_tag: Optional[Literal[
        "payment_issue", "delivery_delay", "damaged_item", "address_change", "refund", "other",
    ]] = Field(
        default=None,
        description="The category of issue the customer's message is about, if any. None if unclear.",
    )


# One example per field being set, plus one example of both being None -
# same "one example per label" philosophy as intent_classifier.py's
# few-shot set, scaled down to this smaller 2-field schema.
_FEW_SHOT_EXAMPLES = [
    ("Why was my order cancelled, I think it was a payment problem",
     RetrievalFilters(order_status="Cancelled", issue_tag="payment_issue")),
    ("My package has been stuck for days, is it delayed?",
     RetrievalFilters(order_status=None, issue_tag="delivery_delay")),
    ("The item that arrived is broken, I want a refund",
     RetrievalFilters(order_status=None, issue_tag="damaged_item")),
    # "arrived" describes an EVENT, not a confirmed current status - this
    # exact phrasing was observed to get misread as order_status=Delivered,
    # which then filtered out the real matching record (status was
    # actually Cancelled, after the damage was reported) and returned zero
    # results. issue_tag is still set here since "broken" unambiguously
    # states the issue category, unlike the status.
    ("My item arrived broken, what happens now?",
     RetrievalFilters(order_status=None, issue_tag="damaged_item")),
    ("Can you tell me more about your company?",
     RetrievalFilters(order_status=None, issue_tag=None)),
]


def extract_retrieval_filters(message: str) -> RetrievalFilters:
    """Runs the extraction. Called by rag_node.py before querying Qdrant - never called with no network/API available, same cost profile as classify_intent_node."""
    messages = []
    for question, answer in _FEW_SHOT_EXAMPLES:
        messages.append({"role": "user", "content": question})
        messages.append({"role": "assistant", "content": answer.model_dump_json()})
    messages.append({"role": "user", "content": message})

    return _client.messages.create(
        model="claude-sonnet-4-6",
        max_tokens=512,
        response_model=RetrievalFilters,
        messages=messages,
    )
