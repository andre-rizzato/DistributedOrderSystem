# src/AgentService/tests/rag_eval/test_rag_quality.py
#
# RAGAS-based retrieval quality gate - port of CursoClaude's
# week15/avaliacao_ragas.py pattern, applied to this service's real graph
# instead of a toy corpus. Marked `rag_eval` (see pytest.ini) and excluded
# from the default `pytest` run - every case here calls Claude (via the
# graph) AND Claude again (via the RAGAS judge), real API cost, and
# LLM-judge variance makes these unsuitable as a hard gate on every push.
#
# Run explicitly: pytest -m rag_eval -v
#
# Eval dataset lesson learned the hard way in the course (week15 v1->v2):
# every question in eval_dataset.jsonl has a ground_truth drawn from an
# ACTUAL seed_data record - evaluating faithfulness against a corpus whose
# "right" document has no real answer in it makes every score degenerate
# to near-zero, which looks like a retrieval bug but is actually a dataset
# bug. Don't repeat that mistake here.

import asyncio
import json
from pathlib import Path

import pytest
from anthropic import AsyncAnthropic
from dotenv import load_dotenv
from ragas.llms.base import llm_factory
from ragas.metrics.collections import ContextPrecisionWithoutReference, Faithfulness

load_dotenv()

from config import load_secrets_from_key_vault  # noqa: E402 - needs load_dotenv() first

load_secrets_from_key_vault()

from graph import build_graph  # noqa: E402 - needs secrets loaded first

_EVAL_DATASET_PATH = Path(__file__).parent / "eval_dataset.jsonl"

# Known Anthropic/Instructor incompatibility, first found running this
# exact pattern in the course (week15) and found AGAIN here in a sharper
# form: ragas's InstructorModelArgs always sends temperature AND top_p by
# default. With the course's older anthropic SDK pin (0.125.0), only
# sending BOTH together broke (HTTP 400). With the anthropic version this
# service actually resolved to (1.12.0 - verified via
# inspect.signature(AsyncMessages.create), which no longer has
# `temperature` OR `top_p` as parameters at all - a newer API shape,
# `output_config`/`thinking`/etc. replaced them), sending EITHER one breaks.
# model_args is a plain dict on the instance, safe to mutate after
# construction - no need to patch the installed package either way.
_ragas_llm = llm_factory("claude-sonnet-4-6", provider="anthropic", client=AsyncAnthropic())
_ragas_llm.model_args.pop("top_p", None)
_ragas_llm.model_args.pop("temperature", None)

_faithfulness = Faithfulness(llm=_ragas_llm)
_context_precision = ContextPrecisionWithoutReference(llm=_ragas_llm)

_compiled_graph = build_graph()

# Minimum acceptable scores - not 1.0, because both metrics are an LLM
# judging another LLM's output, which carries real variance (see the
# course's own week15 finding: the SAME two inputs, scored twice, can come
# back with different Faithfulness numbers). These thresholds catch a
# pipeline that's clearly broken, not every point of natural noise.
_MIN_FAITHFULNESS = 0.7
_MIN_CONTEXT_PRECISION = 0.5


def _load_eval_dataset() -> list[dict]:
    with _EVAL_DATASET_PATH.open(encoding="utf-8") as f:
        return [json.loads(line) for line in f if line.strip()]


def _run_pipeline(case: dict) -> tuple[str, list[str]]:
    """
    Invokes the REAL graph (not a mock of retrieval) with the case's
    question and requester_phone, and returns (reply, retrieved_texts) -
    the same two things RAGAS needs to score Faithfulness and Context
    Precision. Using the full graph, not just rag.retrieval.retrieve()
    directly, means this test also exercises intent classification and
    generate_reply_node's grounding - the thing actually deployed, not a
    narrower slice of it.
    """
    state = {
        "message": case["question"],
        "session_id": "rag-eval",
        "requester_phone": case["requester_phone"],
        "intent": None,
        "order_number": None,
        "confidence": None,
        "order_data": None,
        "final_reply": None,
        "retrieved_context": None,
    }
    result = _compiled_graph.invoke(state)
    retrieved = result.get("retrieved_context") or []
    return result["final_reply"], [chunk["text"] for chunk in retrieved]


@pytest.mark.rag_eval
@pytest.mark.parametrize("case", _load_eval_dataset(), ids=lambda c: c["question"][:40])
def test_faithfulness_and_context_precision(case):
    reply, retrieved_texts = _run_pipeline(case)

    # No context retrieved at all means nothing for RAGAS to score against
    # - this only happens for the dataset's no-identity cases if something
    # upstream is broken (every row here has either a requester_phone or is
    # a public FAQ question, both of which should retrieve something), so
    # treat it as a hard failure rather than skipping silently.
    assert retrieved_texts, f"No context retrieved for: {case['question']!r}"

    faithfulness_result = asyncio.run(
        _faithfulness.ascore(user_input=case["question"], response=reply, retrieved_contexts=retrieved_texts)
    )
    precision_result = asyncio.run(
        _context_precision.ascore(user_input=case["question"], response=reply, retrieved_contexts=retrieved_texts)
    )

    print(f"\n  question={case['question']!r}")
    print(f"  faithfulness={faithfulness_result.value:.4f}  context_precision={precision_result.value:.4f}")

    assert faithfulness_result.value >= _MIN_FAITHFULNESS
    assert precision_result.value >= _MIN_CONTEXT_PRECISION
