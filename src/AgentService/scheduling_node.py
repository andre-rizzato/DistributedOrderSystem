# src/AgentService/scheduling_node.py
# Graph workers for the scheduling capability (calendar integration,
# 2026-10-11) - same role rag_node.py plays for the Order History RAG
# Agent: the LangGraph nodes that actually touch a backend, kept in their
# own module instead of growing graph.py, since this is a self-contained
# capability with its own lazy-loading story (see _get_backend below).
#
# Three nodes, one per thing the chatbot is allowed to do automatically:
#   - check_availability_node: read-only, always safe, always via generate_reply.
#   - book_appointment_node: the one node that can MUTATE a calendar. Two
#     possible shapes of success (see connectors/base.py's
#     SchedulingBackend.book() docstring) - a real confirmed booking
#     (Google/Microsoft) or a Calendly scheduling link - and book_appointment_node
#     already builds the final reply itself for the link case, since there's
#     nothing left for the LLM to decide (same principle as
#     cancel_order_agent_node in graph.py never calling the model for a
#     fixed outcome).
#   - cancel_reschedule_appointment_node: NEVER touches the backend - see
#     connectors/base.py's SchedulingBackend.cancel() docstring for why
#     this mirrors cancel_order_agent_node exactly (destructive, no
#     reliable identity check on every channel, always human handoff).

from typing import Optional

from connectors import get_scheduling_backend
from connectors.base import SchedulingBackend
from messages import message
from state import AgentState

# Preâmbulo: o SchedulingBackend é construído SOB DEMANDA (na primeira vez
# que uma destas três funções de fato precisa dele), nunca no import deste
# módulo - diferente de order_backend em graph.py (sempre presente, toda
# instalação do AgentService atende pedidos). "scheduling" é uma capacidade
# OPCIONAL por cliente (ver agentConfig.enabledCapabilities do lado Node) -
# construir o backend eagerly aqui exigiria GOOGLE_SERVICE_ACCOUNT_FILE (ou
# as credenciais do Microsoft/Calendly) presentes em QUALQUER VM, mesmo uma
# que nunca vai usar agendamento, e derrubaria o boot do processo todo por
# uma env var que nem é dele. `_unavailable` guarda que a tentativa já
# falhou (credencial ausente/errada) pra não tentar de novo a cada mensagem
# - uma falha de configuração não é transitória, só vira transitória quando
# alguém corrige o .env e reinicia o processo (ponto em que este cache some
# de qualquer forma).
_backend_cache: Optional[SchedulingBackend] = None
_unavailable = False


def _get_backend() -> Optional[SchedulingBackend]:
    global _backend_cache, _unavailable
    if _backend_cache is not None:
        return _backend_cache
    if _unavailable:
        return None
    try:
        _backend_cache = get_scheduling_backend()
    except KeyError as exc:
        # Env var obrigatória ausente (ex.: GOOGLE_SERVICE_ACCOUNT_FILE,
        # MICROSOFT_CLIENT_SECRET, CALENDLY_API_TOKEN) - erro de
        # configuração, não um bug do cliente que mandou a mensagem.
        print(f"  [scheduling_node] SchedulingBackend misconfigured, missing env var: {exc!r}")
        _unavailable = True
        return None
    return _backend_cache


def check_availability_node(state: AgentState) -> dict:
    service_id = state.get("service_id")
    requested_date = state.get("requested_date")
    backend = _get_backend()
    if backend is None:
        # None (não []) sinaliza pra generate_reply_node "a capacidade está
        # fora do ar", diferente de uma lista vazia de verdade ("não há
        # horário livre nesse dia") - mesma distinção que order_data/
        # retrieved_context já fazem entre None e "achei, mas está vazio".
        return {"scheduling_result": None}
    try:
        slots = backend.check_availability(service_id, requested_date)
    except Exception as exc:  # network/4xx/5xx do provedor - nunca propaga pro cliente
        print(f"  [NODE check_availability] backend error: {exc!r}")
        return {"scheduling_result": None}
    print(f"  [NODE check_availability] service_id={service_id} date={requested_date} -> {len(slots)} slot(s)")
    return {"scheduling_result": slots}


def book_appointment_node(state: AgentState) -> dict:
    service_id = state.get("service_id")
    slot = state.get("requested_slot")
    # requester_phone (mesma plumbing de segurança do order_info_agent_node)
    # vai pro backend como o único dado de contato que todo canal pode
    # oferecer hoje - nenhum conector usa isso pra verificar identidade
    # ainda (não há "dono" prévio do agendamento a verificar, diferente de
    # um pedido já existente), só registra no evento/convite criado.
    customer = {"phone": state.get("requester_phone")}

    backend = _get_backend()
    if backend is None:
        return {"scheduling_result": None}

    try:
        result = backend.book(service_id, slot, customer)
    except Exception as exc:
        print(f"  [NODE book_appointment] backend error: {exc!r}")
        return {"scheduling_result": None}

    if result is None:
        # Horário ocupado (Google: 409) ou service_id desconhecido - não é
        # erro, é "não deu". `booking_failed` (não None puro) distingue
        # isso de "a capacidade está fora do ar" (ver _get_backend() acima
        # e o branch de None em generate_reply_node, graph.py) - são duas
        # mensagens bem diferentes pro cliente ouvir.
        return {"scheduling_result": {"booking_failed": True}}

    if "scheduling_link" in result:
        # CalendlyBackend (ver connectors/base.py's SchedulingBackend.book()
        # docstring): não há evento confirmado pra formatar, só um link -
        # a resposta final já sai montada AQUI, sem chamar o LLM, porque não
        # há nada pro modelo decidir (mesmo principio de
        # cancel_order_agent_node em graph.py). route_after_booking (abaixo)
        # é quem manda o grafo direto pro END quando final_reply já existe.
        link = result["scheduling_link"]
        print(f"  [NODE book_appointment] scheduling_link issued (Calendly-style backend)")
        return {
            "scheduling_result": result,
            "final_reply": message("scheduling_link", state.get("language")).format(url=link),
        }

    # Google/Microsoft: evento de verdade já criado ("confirmed": True) -
    # segue pro generate_reply_node formatar a confirmação em linguagem
    # natural, mesmo caminho que order_data já percorre hoje.
    print(f"  [NODE book_appointment] confirmed booking, id={result.get('id')}")
    return {"scheduling_result": result}


def cancel_reschedule_appointment_node(state: AgentState) -> dict:
    # NUNCA chama backend.cancel() (ver connectors/base.py's
    # SchedulingBackend.cancel() docstring) - cancelar/reagendar um
    # agendamento é SEMPRE handoff humano, mesma política de
    # cancel_order_agent_node em graph.py e pelo mesmo motivo: é uma ação
    # destrutiva e este serviço não tem como confirmar com segurança, em
    # todo canal, que quem está pedindo é o dono do agendamento.
    intent = state.get("intent")
    message_key = "cancel_appointment_handoff" if intent == "cancel_appointment" else "reschedule_appointment_handoff"
    print(
        f"  [NODE cancel_reschedule_appointment] intent={intent} "
        f"appointment_id={state.get('appointment_id')} -> human handoff (agent never touches the calendar)"
    )
    return {"final_reply": message(message_key, state.get("language"))}


def route_after_booking(state: AgentState) -> str:
    # Pure function, no model call, no network - mesmo principio de
    # testabilidade de route_by_confidence em graph.py. Decide se
    # book_appointment_node já resolveu a resposta final (Calendly: final_reply
    # já setado acima) ou se ainda precisa passar por generate_reply_node
    # pra formatar um resultado cru (confirmação real ou None).
    return "end" if state.get("final_reply") is not None else "generate_reply"
