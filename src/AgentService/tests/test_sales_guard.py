# src/AgentService/tests/test_sales_guard.py
# Covers verify_sales_reply_prices (sales_guard.py) - a pure function, no
# network, no model call. Mirrors the shape of similar deterministic
# guard tests: valor presente na referência passa, valor inventado
# bloqueia, referência vazia bloqueia qualquer valor.

from sales_guard import verify_sales_reply_prices


def test_price_present_in_reference_data_is_allowed():
    products = [{"id": "fx200", "name": "FX200", "price": 349.9}]
    reply = "O FX200 custa R$ 349,90 e está disponível."
    assert verify_sales_reply_prices(reply, products) == []


def test_price_not_in_reference_data_is_blocked():
    products = [{"id": "fx200", "name": "FX200", "price": 349.9}]
    reply = "Consegui um desconto especial, vai ficar R$ 50,00 pra você."
    unsupported = verify_sales_reply_prices(reply, products)
    assert unsupported == ["50.00"]


def test_multiple_products_each_price_checked_independently():
    products = [
        {"id": "fx100", "name": "FX100", "price": 189.9},
        {"id": "fx200", "name": "FX200", "price": 349.9},
    ]
    reply = "O FX100 custa R$ 189,90 e o FX200 custa R$ 349,90."
    assert verify_sales_reply_prices(reply, products) == []


def test_empty_reference_blocks_any_cited_value():
    reply = "Vai custar R$ 100,00."
    assert verify_sales_reply_prices(reply, None) == ["100.00"]
    assert verify_sales_reply_prices(reply, []) == ["100.00"]


def test_reply_with_no_money_mentions_always_passes():
    products = [{"id": "fx200", "name": "FX200", "price": 349.9}]
    reply = "O FX200 está disponível em estoque."
    assert verify_sales_reply_prices(reply, products) == []


def test_checkout_failure_dict_as_reference_still_works():
    # Formato de sales_result quando ambíguo (ver sales_node.py) - mesma
    # estrutura (dict com lista de candidatos) que generate_reply_node
    # passa pra esta função.
    reference = {"checkout_failed": "ambiguous", "candidates": [
        {"id": "fx100", "price": 189.9}, {"id": "fx200", "price": 349.9},
    ]}
    reply = "Encontrei o FX100 (R$ 189,90) e o FX200 (R$ 349,90) - qual você quer?"
    assert verify_sales_reply_prices(reply, reference) == []
