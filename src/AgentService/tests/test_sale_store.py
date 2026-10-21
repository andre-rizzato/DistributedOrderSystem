# src/AgentService/tests/test_sale_store.py
# Covers SaleStore (sale_store.py) against a real temp file - mesma
# abordagem de test_appointment_store.py, pelo mesmo motivo: o ponto desta
# classe é "um arquivo JSON Lines simples", então exercitar contra um
# arquivo real de verdade É o comportamento real, não um substituto dele.
#
# Run from src/AgentService: pytest

from sale_store import SaleStore


def test_create_then_find_round_trips(tmp_path):
    store = SaleStore(str(tmp_path / "sales.jsonl"))
    code = store.generate_code()

    store.create(
        reference_code=code,
        product_id="fx200",
        quantity=2,
        customer={"phone": "+5511999999999"},
        checkout_url="https://checkout.stripe.com/pay/cs_test_abc123",
        provider_reference_id="cs_test_abc123",
    )

    record = store.find(code)
    assert record is not None
    assert record["reference_code"] == code
    assert record["product_id"] == "fx200"
    assert record["quantity"] == 2
    assert record["checkout_url"] == "https://checkout.stripe.com/pay/cs_test_abc123"
    assert record["provider_reference_id"] == "cs_test_abc123"
    assert "created_at" in record


def test_find_returns_none_for_unknown_reference_code(tmp_path):
    store = SaleStore(str(tmp_path / "sales.jsonl"))
    assert store.find("999999") is None


def test_find_returns_none_before_any_file_exists(tmp_path):
    # Diretório criado no __init__, arquivo só nasce no primeiro create() -
    # find() não deve lançar nesse meio-tempo (mesmo "comportamento
    # honesto" de appointment_store.py pra "não encontrado").
    store = SaleStore(str(tmp_path / "subdir" / "sales.jsonl"))
    assert store.find("000000") is None


def test_generated_codes_are_six_digits_and_unique_against_existing(tmp_path):
    store = SaleStore(str(tmp_path / "sales.jsonl"))
    seen = set()
    for _ in range(20):
        code = store.generate_code()
        assert len(code) == 6
        assert code.isdigit()
        assert code not in seen
        seen.add(code)
        # Persiste antes da próxima geração, senão generate_code não teria
        # nada escrito pra evitar colidir contra.
        store.create(
            reference_code=code, product_id="fx200", quantity=1,
            customer={}, checkout_url="https://example.com/pay", provider_reference_id=None,
        )


def test_create_is_append_only_never_rewrites_existing_lines(tmp_path):
    # Mesmo princípio do audit-log.jsonl do lado Node: um link já entregue
    # ao cliente nunca pode mudar silenciosamente depois.
    store = SaleStore(str(tmp_path / "sales.jsonl"))
    first = store.generate_code()
    store.create(
        reference_code=first, product_id="fx100", quantity=1,
        customer={}, checkout_url="https://example.com/pay/1", provider_reference_id="ref-1",
    )
    second = store.generate_code()
    store.create(
        reference_code=second, product_id="fx200", quantity=3,
        customer={}, checkout_url="https://example.com/pay/2", provider_reference_id="ref-2",
    )

    assert store.find(first)["checkout_url"] == "https://example.com/pay/1"
    assert store.find(second)["checkout_url"] == "https://example.com/pay/2"
