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
    # Idioma do cliente já resolvido pelo canal, no Node orchestrator
    # (promptBuilder.ts tem a mesma lógica pro RAG que roda do lado Node) -
    # "pt" | "en" | "it", ou None quando o canal não informa nenhum (hoje:
    # WhatsApp). Sem isso, generate_reply_node (graph.py) não tinha NENHUM
    # sinal de idioma e respondia sempre em inglês, mesmo pra cliente que
    # escreveu em português - bug encontrado em 07/10/2026 depois que o
    # Order History RAG Agent passou a capturar muito mais mensagens nessa
    # rota (antes, poucas perguntas chegavam até generate_reply_node sem
    # contexto nenhum, então o bug raramente aparecia).
    language: Optional[str] = None


class AgentResponse(BaseModel):
    reply: str
    intent: Optional[str] = None
    confidence: Optional[float] = None
    order_id: Optional[str] = None
    # Scheduling capability (calendar integration, 2026-10-11) - mirrors
    # order_id above: optional, log/audit-only fields the Node orchestrator
    # may record alongside the conversation history, never used by Node to
    # decide flow (that decision is made purely from `intent`, same as
    # cancel_order already works - see orchestrator.ts). appointment_id is
    # the provider's own event id (Google/Microsoft) when book_appointment
    # created a real event; scheduling_link is the single-use Calendly URL
    # when the backend could only hand out a link instead (see
    # connectors/base.py's SchedulingBackend.book() docstring for why the
    # two are mutually exclusive, never both set).
    appointment_id: Optional[str] = None
    scheduling_link: Optional[str] = None
    # appointment_code (13/10/2026): o "protocolo" curto de 6 dígitos
    # (appointment_store.py) que o cliente de fato consegue anotar/repetir -
    # appointment_id (acima) continua sendo o id de evento do provedor
    # (Google/Microsoft), útil pra debug/auditoria, mas ilegível num chat.
    appointment_code: Optional[str] = None


@app.post("/agent/message", response_model=AgentResponse)
def handle_message(request: AgentRequest) -> AgentResponse:
    initial_state = {
        "message": request.message,
        "session_id": request.session_id,
        "requester_phone": request.requester_phone,
        "language": request.language,
        "intent": None,
        "order_number": None,
        "confidence": None,
        "order_data": None,
        # Scheduling capability (calendar integration, 2026-10-11) - see
        # state.py's comments on each field. Initialized to None like every
        # other per-turn field above; classify_intent_node and
        # scheduling_node.py's workers are the only ones that ever fill
        # these in.
        "service_id": None,
        "requested_date": None,
        "requested_slot": None,
        "appointment_id": None,
        "scheduling_result": None,
        "final_reply": None,
        "retrieved_context": None,
    }

    final_state = compiled_graph.invoke(initial_state)

    # scheduling_result só existe pras 2 intents que de fato chamam um
    # SchedulingBackend (check_availability_node/book_appointment_node) - é
    # um dict só quando book_appointment_node rodou (lista de horários
    # livres não tem "id" nem "scheduling_link" pra extrair aqui).
    scheduling_result = final_state.get("scheduling_result")
    appointment_id = scheduling_result.get("id") if isinstance(scheduling_result, dict) else None
    scheduling_link = scheduling_result.get("scheduling_link") if isinstance(scheduling_result, dict) else None
    # appointment_code (13/10/2026): o protocolo curto (appointment_store.py)
    # - só existe junto com appointment_id (reserva confirmada de verdade),
    # nunca com scheduling_link (Calendly não tem reserva pra registrar).
    appointment_code = scheduling_result.get("protocolo") if isinstance(scheduling_result, dict) else None

    return AgentResponse(
        reply=final_state["final_reply"] or "Sorry, I couldn't generate a response.",
        intent=final_state.get("intent"),
        confidence=final_state.get("confidence"),
        order_id=final_state.get("order_number"),
        appointment_id=appointment_id,
        scheduling_link=scheduling_link,
        appointment_code=appointment_code,
    )


@app.get("/health")
def health():
    return {"status": "ok"}
