# src/AgentService/main.py
# HTTP API for the Order Orchestrator Agent. This is called by ChatbotService
# (C#) via PythonAgentChatbotService - the contract here (AgentRequest/
# AgentResponse) is what the C# side needs to map to ChatRequest/ChatResponse.
#
# Run with: uvicorn main:app --reload --port 8100

from typing import Optional

from dotenv import load_dotenv
from fastapi import FastAPI
from pydantic import BaseModel

load_dotenv()

from config import load_secrets_from_key_vault  # noqa: E402 - needs load_dotenv() first

# Must run before `from graph import build_graph` below - graph.py builds
# anthropic_client = Anthropic() and the connectors at import time, both of
# which read os.environ immediately. If this ran after that import, it
# would be too late to matter.
load_secrets_from_key_vault()

from graph import build_graph  # noqa: E402 - needs secrets already loaded

app = FastAPI(title="AgentService - Order Orchestrator Agent")
compiled_graph = build_graph()


class AgentRequest(BaseModel):
    message: str
    session_id: str
    # Security review 2026-10-04, item #4: the phone number of whoever is
    # texting, when the channel has a verified one to offer. The Node
    # orchestrator (agentServiceClient.ts) sends the WhatsApp sender's wa_id
    # here; it's None for Telegram and for anything older than this field,
    # since a Telegram chat id isn't a verified phone number - Optional with
    # a default so existing callers that don't send it don't break.
    requester_phone: Optional[str] = None


class AgentResponse(BaseModel):
    reply: str
    intent: Optional[str] = None
    confidence: Optional[float] = None
    order_id: Optional[str] = None


@app.post("/agent/message", response_model=AgentResponse)
def handle_message(request: AgentRequest) -> AgentResponse:
    initial_state = {
        "message": request.message,
        "session_id": request.session_id,
        "requester_phone": request.requester_phone,
        "intent": None,
        "order_number": None,
        "confidence": None,
        "order_data": None,
        "final_reply": None,
        "retrieved_context": None,
    }

    final_state = compiled_graph.invoke(initial_state)

    return AgentResponse(
        reply=final_state["final_reply"] or "Sorry, I couldn't generate a response.",
        intent=final_state.get("intent"),
        confidence=final_state.get("confidence"),
        order_id=final_state.get("order_number"),
    )


@app.get("/health")
def health():
    return {"status": "ok"}
