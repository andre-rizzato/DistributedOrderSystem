# src/AgentService/tests/test_microsoft_calendar_backend.py
# Unit tests for MicrosoftCalendarBackend (connectors/scheduling_microsoft_calendar.py)
# with no real Azure AD/Graph credential and no network: `_access_token`/
# `_client` are monkeypatched directly on the instance, same idea as
# test_scheduling_slots.py testing compute_available_slots() in isolation,
# just one layer up (the connector method, not only the pure function it
# delegates to). httpx.Response objects are built in-memory (no new mocking
# dependency needed - httpx is already a project requirement).
#
# Run from src/AgentService: pytest

from unittest.mock import MagicMock

import httpx

from connectors.scheduling_microsoft_calendar import MicrosoftCalendarBackend


def _fake_client_returning(method: str, response: httpx.Response) -> MagicMock:
    # Monta um MagicMock que se comporta como o `with self._client() as client:`
    # do backend - __enter__ devolve o "client" de verdade que o código
    # chama (.get/.post/.delete), __exit__ não precisa fazer nada.
    fake_client = MagicMock()
    getattr(fake_client.__enter__.return_value, method).return_value = response
    return fake_client


def _backend() -> MicrosoftCalendarBackend:
    backend = MicrosoftCalendarBackend(
        tenant_id="fake-tenant",
        client_id="fake-client",
        client_secret="fake-secret",
        user_id="dona@example.com",
        services={"corte": 30},
        business_hours=(9, 18),
        slot_minutes=30,
    )
    # Evita qualquer tentativa de bater no Azure AD de verdade - token.py
    # próprio já é testado implicitamente (é só um POST simples), o que
    # importa aqui é o que o backend FAZ com o token, não como ele o obtém.
    backend._access_token = lambda: "fake-token"  # noqa: SLF001 - test seam
    return backend


def test_check_availability_reuses_compute_available_slots_via_calendarview():
    # Um evento de 1h ocupando 10:00-11:00 deve remover exatamente os slots
    # de 30min que colidem com ele - mesma matemática do equivalente no
    # Google (test_scheduling_slots.py): 9h de expediente = 18 slots de
    # 30min back-to-back, menos 2 que colidem com o evento = 16.
    payload = {
        "value": [
            {
                "start": {"dateTime": "2026-10-06T10:00:00"},
                "end": {"dateTime": "2026-10-06T11:00:00"},
            }
        ]
    }
    backend = _backend()
    backend._client = lambda: _fake_client_returning(
        "get", httpx.Response(200, json=payload, request=httpx.Request("GET", "x"))
    )

    slots = backend.check_availability("corte", "2026-10-06")

    assert len(slots) == 16
    starts = {slot["start"] for slot in slots}
    assert "2026-10-06T10:00:00" not in starts
    assert "2026-10-06T10:30:00" not in starts
    assert "2026-10-06T09:30:00" in starts  # logo antes do evento, livre
    assert "2026-10-06T11:00:00" in starts  # logo depois, livre


def test_check_availability_unknown_service_returns_empty_without_calling_graph():
    backend = _backend()
    # Nenhum _client configurado de propósito - se o código tentasse
    # chamar a rede aqui, o teste quebraria com AttributeError, provando
    # que o "serviço desconhecido" de fato sai ANTES de qualquer chamada.
    assert backend.check_availability("servico-que-nao-existe", "2026-10-06") == []


def test_book_marks_result_as_confirmed():
    payload = {"id": "AAMk-fake-event-id", "webLink": "https://outlook.office.com/..."}
    backend = _backend()
    backend._client = lambda: _fake_client_returning(
        "post", httpx.Response(201, json=payload, request=httpx.Request("POST", "x"))
    )

    result = backend.book("corte", "2026-10-06T10:00:00", {"name": "Cliente", "phone": "+5511999999999"})

    assert result == {"confirmed": True, "id": "AAMk-fake-event-id", "webLink": "https://outlook.office.com/..."}


def test_book_unknown_service_returns_none_without_calling_graph():
    backend = _backend()
    assert backend.book("servico-que-nao-existe", "2026-10-06T10:00:00", {}) is None


def test_cancel_returns_none_on_404():
    backend = _backend()
    backend._client = lambda: _fake_client_returning(
        "delete", httpx.Response(404, request=httpx.Request("DELETE", "x"))
    )
    assert backend.cancel("missing-event-id") is None


def test_cancel_returns_canceled_on_204():
    backend = _backend()
    backend._client = lambda: _fake_client_returning(
        "delete", httpx.Response(204, request=httpx.Request("DELETE", "x"))
    )
    assert backend.cancel("real-event-id") == {"canceled": True, "event_id": "real-event-id"}
