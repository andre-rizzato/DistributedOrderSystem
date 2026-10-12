# src/AgentService/connectors/scheduling_calendly.py
# SchedulingBackend implementation for clients who already run their booking
# page on Calendly - the third calendar connector (Reuso: total), alongside
# GoogleCalendarBackend and MicrosoftCalendarBackend.
#
# Auth: a Personal Access Token generated once by the business owner in
# their own Calendly account (Integrations -> API & Webhooks) - no OAuth
# app, no client secret, no redirect flow. Simplest of the three credential
# models, same "one static value in Key Vault" shape as PAGSEGURO_TOKEN.
#
# WHY THIS CLASS DOESN'T "BOOK" LIKE THE OTHER TWO (decided with the user,
# 2026-10-11): Calendly's public API has no endpoint that creates a
# scheduled event at an arbitrary time on the business's behalf - only the
# invitee, on Calendly's own hosted page, can finish a booking. The API DOES
# let a third party:
#   - read the real available times for an event type
#     (GET /event_type_available_times) - this backs check_availability();
#   - mint a single-use scheduling link scoped to one event type
#     (POST /scheduling_links) - this backs book() AND reschedule, since
#     Calendly has no separate "reschedule" endpoint either: the bot hands
#     out a fresh link both times, the customer picks (or re-picks) a time
#     on Calendly's page;
#   - cancel an existing scheduled event on the organizer's behalf
#     (POST /scheduled_events/{uuid}/cancellation) - this is a REAL direct
#     cancel, unlike book(). Kept on this class for completeness (the
#     Protocol requires it), but see graph.py: the chatbot's own graph never
#     calls it - cancellation is always a human handoff, same policy as
#     every other provider and as order cancellation.
# book()'s return value reflects this: {"scheduling_link": url}, never
# {"confirmed": True} - see connectors/base.py's SchedulingBackend.book()
# docstring for how callers (scheduling_node.py) are expected to branch on
# that.
#
# NOT exercised against a live Calendly account in this session (no test
# token available) - the request/response shapes follow Calendly's
# documented API v2 contract (api.calendly.com), but verify against a real
# account before depending on this for a client.

from datetime import datetime

import httpx

CALENDLY_API_BASE = "https://api.calendly.com"


class CalendlyBackend:
    # event_type_uri: a URI fixa do tipo de evento que o agente vai
    # oferecer (ex.: "https://api.calendly.com/event_types/AAAAAAAAAAAAAAAA")
    # - decisão já tomada com o usuário de ser UMA agenda do negócio, não
    # uma por cliente final, então um só event type cobre o caso de uso de
    # hoje. `services`/service_id do Protocol não se aplicam aqui do mesmo
    # jeito que nos outros dois conectores (o Calendly não tem conceito de
    # "duração configurável por serviço" nesta classe - a duração já está
    # definida NO PRÓPRIO event type, do lado do Calendly) - o parâmetro
    # existe só pra cumprir a mesma assinatura do Protocol, e é ignorado.
    def __init__(self, api_token: str, event_type_uri: str, timeout_seconds: float = 10.0) -> None:
        self.api_token = api_token
        self.event_type_uri = event_type_uri
        self.timeout_seconds = timeout_seconds

    def _client(self) -> httpx.Client:
        return httpx.Client(
            base_url=CALENDLY_API_BASE,
            headers={"Authorization": f"Bearer {self.api_token}"},
            timeout=self.timeout_seconds,
        )

    def check_availability(self, service_id: str, date: str) -> list[dict]:
        # service_id ignorado (ver comentário do __init__) - o event type é
        # fixo por instância desta classe. start_time/end_time são
        # obrigatórios na API do Calendly; usamos o dia inteiro em UTC, já
        # que o próprio endpoint devolve os horários já convertidos pro
        # fuso configurado no event type do Calendly, não um que a gente
        # escolha aqui.
        day_start = datetime.fromisoformat(date).replace(hour=0, minute=0, second=0, microsecond=0)
        day_end = day_start.replace(hour=23, minute=59, second=59)

        with self._client() as client:
            response = client.get(
                "/event_type_available_times",
                params={
                    "event_type": self.event_type_uri,
                    "start_time": day_start.isoformat() + "Z",
                    "end_time": day_end.isoformat() + "Z",
                },
            )
        response.raise_for_status()
        # Cada item já vem como {"status": "available", "start_time": ...,
        # "invitees_remaining": N, "scheduling_url": ...} - filtramos por
        # "available" porque o Calendly também pode incluir horários
        # marcados como indisponíveis dependendo do plano/config.
        collection = response.json().get("collection", [])
        return [
            {"start": slot["start_time"], "scheduling_url": slot.get("scheduling_url")}
            for slot in collection
            if slot.get("status") == "available"
        ]

    def book(self, service_id: str, slot: str, customer: dict) -> dict | None:
        # slot é só informativo aqui (log/auditoria do lado do Node) - o
        # link de uso único abaixo não prende um horário específico; o
        # cliente final escolhe (de novo) entre os horários realmente
        # livres na própria página do Calendly, que são os mesmos que
        # check_availability() já leu. Ver o docstring do topo do arquivo
        # pra reasoning completo - é uma limitação real da API do
        # Calendly, não um atalho tomado aqui.
        payload = {
            "max_event_count": 1,
            "owner": self.event_type_uri,
            "owner_type": "EventType",
        }
        with self._client() as client:
            response = client.post("/scheduling_links", json=payload)
        response.raise_for_status()
        link = response.json()["resource"]["booking_url"]
        return {"scheduling_link": link}

    # reschedule é o MESMO mecanismo que book(): o Calendly não tem um
    # endpoint de "reagendar", então o agente simplesmente gera outro link
    # de uso único - do lado do grafo (scheduling_node.py), a intent
    # reschedule_appointment nem chega a chamar este backend (cancelar/
    # reagendar é sempre handoff humano, ver base.py), mas o método fica
    # aqui disponível pra um atendente/ferramenta futura que precise gerar
    # um novo link depois de confirmar o cancelamento com o cliente.
    def reschedule(self, service_id: str, slot: str, customer: dict) -> dict | None:
        return self.book(service_id, slot, customer)

    def cancel(self, appointment_id: str) -> dict | None:
        # appointment_id aqui é o UUID do scheduled event (a última parte
        # da URI que o Calendly usa pra identificar o evento, ex.:
        # ".../scheduled_events/AAAAAAAA-BBBB-...") - não o uuid completo
        # da URI, só o segmento final, igual Calendly documenta pra este
        # endpoint.
        with self._client() as client:
            response = client.post(
                f"/scheduled_events/{appointment_id}/cancellation",
                json={"reason": "Cancelado a pedido do cliente via agente de atendimento."},
            )
        if response.status_code == 404:
            return None
        response.raise_for_status()
        return {"canceled": True, "event_id": appointment_id}
