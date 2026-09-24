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

from graph import build_graph  # noqa: E402 - needs to come after load_dotenv()

app = FastAPI(title="AgentService - Order Orchestrator Agent")
compiled_graph = build_graph()


class AgentRequest(BaseModel):
    message: str
    session_id: str


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
        "intent": None,
        "order_number": None,
        "confidence": None,
        "order_data": None,
        "final_reply": None,
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
