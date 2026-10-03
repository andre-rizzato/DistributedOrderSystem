# src/AgentService/connectors/scheduling_google_calendar.py
# SchedulingBackend implementation for the "cliente usa Google Agenda" case
# (Reuso: total in the connector table) - one real system, one
# implementation, every client just brings their own service-account
# credential + calendar id. Suits barbearia/salão/clínica, the three
# verticals the design doc maps to SchedulingBackend.
#
# Auth uses google-auth (the official library) instead of hand-rolling the
# service-account JWT signing - that's a security-sensitive operation best
# left to a vetted library, same principle as using `instructor` instead of
# hand-parsing the model's JSON in intent_classifier.py. Everything else
# (freeBusy, events.insert, events.delete) is a plain REST call via httpx,
# consistent with order_rest.py and gateway_client.py before it.
#
# NOT exercised against a live Google Calendar in this session (no test
# service account available) - the request/response shapes follow the
# documented Calendar API v3 contract, but verify against a real calendar
# before depending on this for a client.

from datetime import datetime, timedelta

import httpx
from google.auth.transport.requests import Request as GoogleAuthRequest
from google.oauth2 import service_account

SCOPES = ["https://www.googleapis.com/auth/calendar"]
CALENDAR_API_BASE = "https://www.googleapis.com/calendar/v3"


def compute_available_slots(
    day_start: datetime,
    day_end: datetime,
    busy_ranges: list[tuple[datetime, datetime]],
    service_duration: timedelta,
    step: timedelta,
) -> list[dict]:
    # Pulled out of check_availability() on purpose - same principle
    # graph.py's route_by_confidence() already follows (see its own
    # comment): the actual slot-picking logic is a pure function, so
    # tests/test_scheduling_slots.py can exercise every edge case (back-to-
    # back bookings, a slot that starts before a busy block but would run
    # into it, a fully-booked day) with a fake busy_ranges list - no
    # network, no Google credential, microseconds per test.
    slots: list[dict] = []
    cursor = day_start
    while cursor + service_duration <= day_end:
        slot_end = cursor + service_duration
        overlaps_busy = any(
            cursor < busy_end and slot_end > busy_start for busy_start, busy_end in busy_ranges
        )
        if not overlaps_busy:
            slots.append({"start": cursor.isoformat(), "end": slot_end.isoformat()})
        cursor += step
    return slots


class GoogleCalendarBackend:
    def __init__(
        self,
        service_account_file: str,
        calendar_id: str,
        services: dict[str, int],
        business_hours: tuple[int, int] = (9, 18),
        slot_minutes: int = 30,
        timezone_name: str = "America/Sao_Paulo",
    ) -> None:
        # services maps service_id -> duration in minutes (e.g. {"corte": 30,
        # "coloracao": 90}) - the client defines this in their own config,
        # same spirit as agent.config.json on the Node side.
        self._credentials = service_account.Credentials.from_service_account_file(
            service_account_file, scopes=SCOPES
        )
        self.calendar_id = calendar_id
        self.services = services
        self.business_start_hour, self.business_end_hour = business_hours
        self.slot_minutes = slot_minutes
        self.timezone_name = timezone_name

    def _access_token(self) -> str:
        if not self._credentials.valid:
            self._credentials.refresh(GoogleAuthRequest())
        return self._credentials.token

    def _client(self) -> httpx.Client:
        return httpx.Client(
            base_url=CALENDAR_API_BASE,
            headers={"Authorization": f"Bearer {self._access_token()}"},
            timeout=10.0,
        )

    def check_availability(self, service_id: str, date: str) -> list[dict]:
        duration = self.services.get(service_id)
        if duration is None:
            # Unknown service id - nothing to offer, not an error (mirrors
            # RestOrderBackend returning None for "not found" rather than
            # raising, so the calling node doesn't need a special case).
            return []

        day_start = datetime.fromisoformat(date).replace(
            hour=self.business_start_hour, minute=0, second=0, microsecond=0
        )
        day_end = day_start.replace(hour=self.business_end_hour)

        with self._client() as client:
            response = client.post(
                "/freeBusy",
                json={
                    "timeMin": day_start.isoformat(),
                    "timeMax": day_end.isoformat(),
                    "timeZone": self.timezone_name,
                    "items": [{"id": self.calendar_id}],
                },
            )
        response.raise_for_status()
        busy_blocks = response.json()["calendars"][self.calendar_id]["busy"]
        busy_ranges = [
            (datetime.fromisoformat(b["start"]), datetime.fromisoformat(b["end"]))
            for b in busy_blocks
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
            "summary": f"{customer.get('name', 'Cliente')} — {service_id}",
            "description": f"Agendado via agente. Contato: {customer.get('phone') or customer.get('email', '')}",
            "start": {"dateTime": start.isoformat(), "timeZone": self.timezone_name},
            "end": {"dateTime": end.isoformat(), "timeZone": self.timezone_name},
        }
        with self._client() as client:
            response = client.post(f"/calendars/{self.calendar_id}/events", json=event)
        if response.status_code == 409:
            # Google's own conflict signal if two bookings race the same slot.
            return None
        response.raise_for_status()
        return response.json()

    def cancel(self, appointment_id: str) -> dict | None:
        with self._client() as client:
            response = client.delete(f"/calendars/{self.calendar_id}/events/{appointment_id}")
        if response.status_code == 404:
            return None
        if response.status_code not in (200, 204):
            response.raise_for_status()
        return {"canceled": True, "event_id": appointment_id}
