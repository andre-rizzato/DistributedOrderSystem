# src/AgentService/tests/test_appointment_store.py
# Covers AppointmentStore (appointment_store.py) against a real temp file -
# no mocking needed, since the whole point of this class is "a plain JSON
# Lines file", so exercising it against an actual file on disk IS the
# real behavior, not a stand-in for it.
#
# Run from src/AgentService: pytest

from appointment_store import AppointmentStore


def test_create_then_find_round_trips(tmp_path):
    store = AppointmentStore(str(tmp_path / "appointments.jsonl"))
    protocolo = store.generate_protocolo()

    store.create(
        protocolo=protocolo,
        provider="google_calendar",
        service_id="corte",
        slot="2026-10-20T15:00:00",
        customer={"phone": "+5511999999999", "protocolo": protocolo},
        backend_event_id="google-event-abc123",
    )

    record = store.find(protocolo)
    assert record is not None
    assert record["protocolo"] == protocolo
    assert record["provider"] == "google_calendar"
    assert record["service_id"] == "corte"
    assert record["backend_event_id"] == "google-event-abc123"
    assert "created_at" in record


def test_find_returns_none_for_unknown_protocolo(tmp_path):
    store = AppointmentStore(str(tmp_path / "appointments.jsonl"))
    assert store.find("999999") is None


def test_find_returns_none_before_any_file_exists(tmp_path):
    # O diretório é criado no __init__ (ver appointment_store.py), mas o
    # ARQUIVO só nasce no primeiro create() - find() não deve lançar nesse
    # meio-tempo, mesmo "comportamento honesto" que o resto do projeto
    # segue pra "não encontrado" (nunca um erro).
    store = AppointmentStore(str(tmp_path / "subdir" / "appointments.jsonl"))
    assert store.find("000000") is None


def test_generated_protocolos_are_six_digits_and_unique_against_existing(tmp_path):
    store = AppointmentStore(str(tmp_path / "appointments.jsonl"))
    seen = set()
    for _ in range(20):
        protocolo = store.generate_protocolo()
        assert len(protocolo) == 6
        assert protocolo.isdigit()
        assert protocolo not in seen
        seen.add(protocolo)
        # Persiste antes da próxima geração, senão generate_protocolo não
        # teria nada escrito pra evitar colidir contra.
        store.create(
            protocolo=protocolo,
            provider="google_calendar",
            service_id="corte",
            slot="2026-10-20T15:00:00",
            customer={},
            backend_event_id=None,
        )


def test_create_is_append_only_never_rewrites_existing_lines(tmp_path):
    # Mesmo princípio do audit-log.jsonl do lado Node: um registro já
    # entregue ao cliente nunca pode mudar silenciosamente depois.
    store = AppointmentStore(str(tmp_path / "appointments.jsonl"))
    first = store.generate_protocolo()
    store.create(
        protocolo=first, provider="google_calendar", service_id="corte",
        slot="2026-10-20T15:00:00", customer={}, backend_event_id="evt-1",
    )
    second = store.generate_protocolo()
    store.create(
        protocolo=second, provider="google_calendar", service_id="coloracao",
        slot="2026-10-21T10:00:00", customer={}, backend_event_id="evt-2",
    )

    assert store.find(first)["backend_event_id"] == "evt-1"
    assert store.find(second)["backend_event_id"] == "evt-2"
