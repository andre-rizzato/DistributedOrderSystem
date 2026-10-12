# src/AgentService/connectors/scheduling_microsoft_calendar.py
# SchedulingBackend implementation for the "cliente usa Microsoft 365 /
# Outlook" case - same connector-table slot as GoogleCalendarBackend (Reuso:
# total), same verticals (barbearia/salão/clínica).
#
# Auth: OAuth2 client-credentials ("app-only"), NOT the interactive
# authorization-code flow a human clicks through. An Azure AD app
# registration with an *Application* permission (Calendars.ReadWrite,
# admin-consented once) can read/write ANY mailbox's calendar in the tenant
# just by knowing its user id/UPN - no per-conversation redirect, no refresh
# token to store, same "one click from the business owner, shared by every
# conversation" shape the Google service-account connector already has (see
# CLAUDE.md's decision to keep the admin login screen's OAuth model separate
# from this one - that one IS interactive on purpose, this one deliberately
# is not). The client secret itself still has to live in Key Vault like any
# other credential (see config.py's SECRET_ENV_VARS).
#
# Busy-time lookup reuses compute_available_slots() from
# scheduling_google_calendar.py instead of duplicating it - the slot-picking
# logic (which half-hour blocks are free given a list of busy ranges) is
# identical between providers; only the network call that produces
# `busy_ranges` differs (Google's /freeBusy vs Graph's /calendarView).
#
# NOT exercised against a live Microsoft 365 tenant in this session (no test
# app registration available) - the request/response shapes follow the
# documented Microsoft Graph v1.0 contract (/calendarView, /events,
# /oauth2/v2.0/token), but verify against a real tenant before depending on
# this for a client.

import time
from datetime import datetime, timedelta

import httpx

from .scheduling_google_calendar import compute_available_slots

GRAPH_API_BASE = "https://graph.microsoft.com/v1.0"
# .default on an app-only token means "whatever Application permissions the
# admin already consented to for this app registration" - Graph's own
# recommended scope for client-credentials, as opposed to listing individual
# scopes like the interactive flow does.
GRAPH_SCOPE = "https://graph.microsoft.com/.default"


class MicrosoftCalendarBackend:
    # Preâmbulo (mantendo a convenção do construtor do GoogleCalendarBackend
    # acima): tenant_id/client_id/client_secret identificam o App
    # registration no Azure AD; user_id é o dono da agenda (UPN ou GUID do
    # usuário do Microsoft 365) - um valor fixo por cliente, não por
    # conversa, porque a decisão já tomada com o usuário é "agenda do
    # negócio, compartilhada", nunca uma agenda por cliente final.
    def __init__(
        self,
        tenant_id: str,
        client_id: str,
        client_secret: str,
        user_id: str,
        services: dict[str, int],
        business_hours: tuple[int, int] = (9, 18),
        slot_minutes: int = 30,
        timezone_name: str = "America/Sao_Paulo",
    ) -> None:
        self.tenant_id = tenant_id
        self.client_id = client_id
        self.client_secret = client_secret
        self.user_id = user_id
        self.services = services
        self.business_start_hour, self.business_end_hour = business_hours
        self.slot_minutes = slot_minutes
        self.timezone_name = timezone_name
        # Cache do access token em memória, mesmo raciocínio do
        # GoogleCalendarBackend._access_token(): evita um round-trip ao
        # Azure AD a cada chamada de /api/config. Guardamos o timestamp de
        # expiração (não o objeto de credencial inteiro, como o Google faz,
        # porque aqui a renovação é manual via REST, não uma lib oficial).
        self._token: str | None = None
        self._token_expires_at: float = 0.0

    def _access_token(self) -> str:
        # 60s de margem antes do vencimento real - mesma ideia de
        # CLOCK_SKEW_SECONDS no login de admin (src/admin/firebaseAuth.ts do
        # lado Node): preferível renovar um pouco antes da hora do que
        # arriscar usar um token que o Azure AD já rejeitou.
        if self._token and time.monotonic() < self._token_expires_at - 60:
            return self._token
        token_url = f"https://login.microsoftonline.com/{self.tenant_id}/oauth2/v2.0/token"
        response = httpx.post(
            token_url,
            data={
                "grant_type": "client_credentials",
                "client_id": self.client_id,
                "client_secret": self.client_secret,
                "scope": GRAPH_SCOPE,
            },
            timeout=10.0,
        )
        response.raise_for_status()
        body = response.json()
        self._token = body["access_token"]
        self._token_expires_at = time.monotonic() + float(body["expires_in"])
        return self._token

    def _client(self) -> httpx.Client:
        return httpx.Client(
            base_url=GRAPH_API_BASE,
            headers={"Authorization": f"Bearer {self._access_token()}"},
            timeout=10.0,
        )

    def check_availability(self, service_id: str, date: str) -> list[dict]:
        duration = self.services.get(service_id)
        if duration is None:
            # Serviço desconhecido - nada pra oferecer, não é um erro (mesmo
            # raciocínio do GoogleCalendarBackend: o node que chama não
            # precisa de um caso especial pra "serviço não existe").
            return []

        day_start = datetime.fromisoformat(date).replace(
            hour=self.business_start_hour, minute=0, second=0, microsecond=0
        )
        day_end = day_start.replace(hour=self.business_end_hour)

        # /calendarView lista os EVENTOS do intervalo (não um bitmap de
        # ocupado/livre como o freeBusy do Google) - cada evento retornado
        # vira um busy_range, e dali reaproveitamos a mesma função pura que
        # o conector do Google já usa.
        with self._client() as client:
            response = client.get(
                f"/users/{self.user_id}/calendarView",
                params={
                    "startDateTime": day_start.isoformat(),
                    "endDateTime": day_end.isoformat(),
                },
                headers={"Prefer": f'outlook.timezone="{self.timezone_name}"'},
            )
        response.raise_for_status()
        events = response.json().get("value", [])
        busy_ranges = [
            (
                datetime.fromisoformat(event["start"]["dateTime"]),
                datetime.fromisoformat(event["end"]["dateTime"]),
            )
            for event in events
        ]

        return compute_available_slots(
            day_start=day_start,
            day_end=day_end,
            busy_ranges=busy_ranges,
            service_duration=timedelta(minutes=duration),
            step=timedelta(minutes=self.slot_minutes),
        )

    def book(self, service_id: str, slot: str, customer: dict) -> dict | None:
        duration = self.services.get(service_id)
        if duration is None:
            return None

        start = datetime.fromisoformat(slot)
        end = start + timedelta(minutes=duration)
        event = {
            "subject": f"{customer.get('name', 'Cliente')} — {service_id}",
            "body": {
                "contentType": "text",
                "content": f"Agendado via agente. Contato: {customer.get('phone') or customer.get('email', '')}",
            },
            "start": {"dateTime": start.isoformat(), "timeZone": self.timezone_name},
            "end": {"dateTime": end.isoformat(), "timeZone": self.timezone_name},
        }
        with self._client() as client:
            response = client.post(f"/users/{self.user_id}/events", json=event)
        # Graph não devolve 409 por conflito de horário como o Google faz -
        # /events sempre cria o evento, mesmo em cima de outro (diferença
        # real de API, não um descuido daqui). O check_availability() acima
        # é a única proteção contra choque de horário com este provedor; um
        # 4xx aqui é falha de autenticação/payload, não "horário ocupado".
        response.raise_for_status()
        return {"confirmed": True, **response.json()}

    def cancel(self, appointment_id: str) -> dict | None:
        with self._client() as client:
            response = client.delete(f"/users/{self.user_id}/events/{appointment_id}")
        if response.status_code == 404:
            return None
        if response.status_code != 204:
            response.raise_for_status()
        return {"canceled": True, "event_id": appointment_id}
