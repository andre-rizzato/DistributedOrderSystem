# src/AgentService/tests/test_contract_agent_message.py
#
# Guards the ONE thing that must never silently change: the /agent/message
# HTTP contract that ai-customer-service-agent (the Node product) depends
# on. That repo's agentServiceClient.ts comment says it plainly: "this
# module only knows the HTTP contract... AgentService could swap LangGraph
# for anything else without this file changing a line" - these tests are
# what makes that promise actually checkable, not just a comment someone
# has to trust. No `rag_eval` marker - fast, no paid API beyond what
# classify_intent_node already costs on every run, so this runs on every
# deploy exactly like the pre-existing tests/ do today.
#
# Uses FastAPI's TestClient - no real server process needed, no network
# socket, calls straight into the app in-process.

from fastapi.testclient import TestClient

from main import app

client = TestClient(app)


def test_cancel_order_contract_unchanged():
    """
    The Node orchestrator hardcodes a string check for intent ==
    "cancel_order" as an independent safety net - if this node ever starts
    responding with something that isn't exactly "cancel_order" for a
    cancellation request, that Node-side check silently stops firing (see
    orchestrator.ts's comment). Also locks in the fixed handoff message's
    key phrase, so a future refactor of cancel_order_agent_node can't
    accidentally soften or remove it without this test failing first.

    language="en" pinned explicitly (07/10/2026): cancel_order_agent_node's
    reply is now localized (messages.py), so the key phrase only shows up
    in the English copy - without pinning this, the request would fall
    back to DEFAULT_LANGUAGE ("pt", see messages.py) and this assertion
    would fail for a reason unrelated to what this test actually guards.
    """
    response = client.post(
        "/agent/message",
        json={"message": "please cancel order 12345", "session_id": "contract-test-1", "language": "en"},
    )
    assert response.status_code == 200
    body = response.json()
    assert body["intent"] == "cancel_order"
    assert "connecting you with a human agent" in body["reply"]


def test_response_schema_has_no_new_fields():
    """
    retrieved_context (state.py) must never leak into AgentResponse - it's
    an AgentState-only field, consumed by generate_reply_node, never
    serialized. This test fails loudly if a future change accidentally
    starts returning it (or any other new field) over HTTP, which would be
    a contract change Node was never told about.
    """
    response = client.post("/agent/message", json={"message": "hi", "session_id": "contract-test-2"})
    assert response.status_code == 200
    assert set(response.json().keys()) <= {"reply", "intent", "confidence", "order_id"}


def test_order_history_query_without_identity_never_crashes():
    """
    A request with no requester_phone (every Telegram message today, see
    connectors/base.py) asking an order-history-style question must
    degrade to an honest reply, never a 500 - rag_node.py's privacy
    guardrail is a `return`, not a `raise`, and this test is what catches
    it if a future refactor turns it into the latter by accident.
    """
    response = client.post(
        "/agent/message",
        json={"message": "did I have any payment problems with my orders?", "session_id": "contract-test-3"},
    )
    assert response.status_code == 200
    assert response.json()["reply"]  # non-empty - some honest reply came back, not a blank one
