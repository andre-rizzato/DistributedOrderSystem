# src/AgentService/tests/test_calendly_backend.py
# Unit tests for CalendlyBackend (connectors/scheduling_calendly.py) with no
# real Calendly token and no network - same monkeypatch-the-seam approach
# as test_microsoft_calendar_backend.py. The main thing worth locking in
# here is the contract divergence documented in connectors/base.py's
# SchedulingBackend.book() docstring: book() returns {"scheduling_link":
# ...}, NEVER {"confirmed": True} - a future change that quietly tried to
# "confirm" a Calendly booking would be wrong (the API can't actually do
# that), and this test is what would catch it.
#
# Run from src/AgentService: pytest

from unittest.mock import MagicMock

import httpx

from connectors.scheduling_calendly import CalendlyBackend


def _fake_client_returning(method: str, response: httpx.Response) -> MagicMock:
    fake_client = MagicMock()
    getattr(fake_client.__enter__.return_value, method).return_value = response
    return fake_client


def _backend() -> CalendlyBackend:
    return CalendlyBackend(api_token="fake-token", event_type_uri="https://api.calendly.com/event_types/FAKE")


def test_check_availability_keeps_only_available_slots():
    payload = {
        "collection": [
            {"status": "available", "start_time": "2026-10-20T13:00:00Z", "scheduling_url": "https://calendly.com/x/1"},
            {"status": "unavailable", "start_time": "2026-10-20T14:00:00Z", "scheduling_url": "https://calendly.com/x/2"},
        ]
    }
    backend = _backend()
    backend._client = lambda: _fake_client_returning(
        "get", httpx.Response(200, json=payload, request=httpx.Request("GET", "x"))
    )

    slots = backend.check_availability("corte", "2026-10-20")

    assert slots == [{"start": "2026-10-20T13:00:00Z", "scheduling_url": "https://calendly.com/x/1"}]


def test_book_returns_a_scheduling_link_never_a_confirmed_booking():
    payload = {"resource": {"booking_url": "https://calendly.com/d/abc-123", "owner": "https://api.calendly.com/event_types/FAKE"}}
    backend = _backend()
    backend._client = lambda: _fake_client_returning(
        "post", httpx.Response(201, json=payload, request=httpx.Request("POST", "x"))
    )

    result = backend.book("corte", "2026-10-20T13:00:00", {"phone": "+5511999999999"})

    assert result == {"scheduling_link": "https://calendly.com/d/abc-123"}
    assert "confirmed" not in result  # ver docstring do teste: nunca finge que confirmou


def test_reschedule_is_the_same_mechanism_as_book():
    payload = {"resource": {"booking_url": "https://calendly.com/d/xyz-456"}}
    backend = _backend()
    backend._client = lambda: _fake_client_returning(
        "post", httpx.Response(201, json=payload, request=httpx.Request("POST", "x"))
    )

    assert backend.reschedule("corte", "2026-10-21T10:00:00", {}) == {"scheduling_link": "https://calendly.com/d/xyz-456"}


def test_cancel_returns_none_on_404():
    backend = _backend()
    backend._client = lambda: _fake_client_returning(
        "post", httpx.Response(404, request=httpx.Request("POST", "x"))
    )
    assert backend.cancel("missing-uuid") is None


def test_cancel_returns_canceled_on_success():
    backend = _backend()
    backend._client = lambda: _fake_client_returning(
        "post", httpx.Response(200, json={}, request=httpx.Request("POST", "x"))
    )
    assert backend.cancel("real-uuid") == {"canceled": True, "event_id": "real-uuid"}
