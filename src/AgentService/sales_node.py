# src/AgentService/sales_node.py
# Graph workers for the sales capability (catalog + checkout, 2026-10-20) -
# same role scheduling_node.py plays for the scheduling capability: the
# LangGraph nodes that actually touch a backend, kept in their own module
# instead of growing graph.py.
#
# Only TWO nodes (scheduling needed three) - there is no bot-triggered
# destructive action in sales to guard against. cancel_order_agent_node and
# cancel_reschedule_appointment_node exist because this service CAN
# irreversibly change something real (cancel an order, cancel a booking)
# and deliberately refuses to. Sales never reaches that point: the agent
# never charges a card, it only ever hands out a checkout link - Stripe and
# PagSeguro keep full control of the actual payment on their own hosted
# page (see connectors/base.py's CheckoutBackend docstring). There is
# nothing here that needs a "never call backend.X()" handoff guard, because
# there is no equivalent of backend.cancel() in CheckoutBackend at all.
#
#   - sales_check_product_node: read-only, always safe, always via
#     generate_reply (same shape as check_availability_node).
#   - sales_start_checkout_node: resolves product_query to exactly ONE
#     product, then asks the CheckoutBackend for a hosted link. Builds the
#     final reply itself when it succeeds (same principle as
#     book_appointment_node's Calendly branch - nothing left for the LLM to
#     decide, the message is always the same shape with the link swapped
#     in) OR when it can't resolve to a single product (ambiguous/not
#     found/misconfigured price) it leaves final_reply unset and lets
#     generate_reply_node phrase the honest outcome from sales_result.

import os
from typing import Optional

from connectors import get_catalog_backend, get_checkout_backend
from connectors.base import CatalogBackend, CheckoutBackend
from messages import message
from sale_store import SaleStore
from state import AgentState

# Preâmbulo: catálogo e checkout são construídos SOB DEMANDA, nunca no
# import deste módulo - mesma razão de _get_backend() em
# scheduling_node.py: "sales" é uma capacidade OPCIONAL por cliente (ver
# agentConfig.enabledCapabilities do lado Node), e construir os backends
# eagerly aqui exigiria CATALOG_API_BASE_URL/STRIPE_SECRET_KEY/
# PAGSEGURO_TOKEN presentes em QUALQUER VM, mesmo uma que nunca vende nada
# pelo bot. `_catalog_unavailable`/`_checkout_unavailable` guardam que a
# tentativa já falhou pra não tentar de novo a cada mensagem - só reseta
# reiniciando o processo (quando alguém corrige o .env).
#
# Diferente de scheduling_node.py, o except aqui cobre KeyError E ValueError
# (não só KeyError): get_checkout_backend() pode levantar ValueError se
# CHECKOUT_PROCESSOR tiver um valor desconhecido (não só faltando), e
# qualquer um dos dois erros de configuração deve degradar pra "capacidade
# indisponível" do mesmo jeito, nunca derrubar o processo.
_catalog_cache: Optional[CatalogBackend] = None
_catalog_unavailable = False
_checkout_cache: Optional[CheckoutBackend] = None
_checkout_unavailable = False


def _get_catalog_backend() -> Optional[CatalogBackend]:
    global _catalog_cache, _catalog_unavailable
    if _catalog_cache is not None:
        return _catalog_cache
    if _catalog_unavailable:
        return None
    try:
        _catalog_cache = get_catalog_backend()
    except (KeyError, ValueError) as exc:
        print(f"  [sales_node] CatalogBackend misconfigured: {exc!r}")
        _catalog_unavailable = True
        return None
    return _catalog_cache


def _get_checkout_backend() -> Optional[CheckoutBackend]:
    global _checkout_cache, _checkout_unavailable
    if _checkout_cache is not None:
        return _checkout_cache
    if _checkout_unavailable:
        return None
    try:
        _checkout_cache = get_checkout_backend()
    except (KeyError, ValueError) as exc:
        print(f"  [sales_node] CheckoutBackend misconfigured: {exc!r}")
        _checkout_unavailable = True
        return None
    return _checkout_cache


# Preâmbulo: o SaleStore (ver sale_store.py) é construído sob demanda igual
# aos backends acima, mas por um motivo mais simples (mesmo de
# _get_appointment_store() em scheduling_node.py): não exige credencial
# nenhuma, só precisa existir antes do primeiro checkout bem-sucedido.
_sale_store_cache: Optional[SaleStore] = None


def _get_sale_store() -> SaleStore:
    global _sale_store_cache
    if _sale_store_cache is None:
        _sale_store_cache = SaleStore(os.getenv("SALES_STORE_PATH", "data/sales.jsonl"))
    return _sale_store_cache


# Preâmbulo: _price_to_cents() converte o preço cru que o CatalogBackend
# devolveu (normalmente um float/str em reais, ex. 349.9) pro formato que
# StripeCheckoutBackend/PagSeguroCheckoutBackend exigem
# (unit_amount_cents, inteiro, menor unidade da moeda - ver
# connectors/sales_payment_link.py). None quando o campo está ausente ou
# não é um número válido - sales_start_checkout_node trata isso como falha
# de resolução (checkout_failed: "invalid_price"), nunca chuta um preço.
def _price_to_cents(raw_price) -> Optional[int]:
    if raw_price is None:
        return None
    try:
        return round(float(raw_price) * 100)
    except (TypeError, ValueError):
        return None


def sales_check_product_node(state: AgentState) -> dict:
    product_query = state.get("product_query")
    backend = _get_catalog_backend()
    if backend is None:
        # None (não []) sinaliza "capacidade fora do ar" pra generate_reply_node,
        # mesma distinção de scheduling_result/order_data entre None e uma
        # lista vazia de verdade ("não achei esse produto").
        return {"sales_result": None}
    try:
        products = backend.search_products(product_query)
    except Exception as exc:  # network/4xx/5xx do provedor - nunca propaga pro cliente
        print(f"  [NODE sales_check_product] backend error: {exc!r}")
        return {"sales_result": None}
    print(f"  [NODE sales_check_product] product_query={product_query!r} -> {len(products)} result(s)")
    return {"sales_result": products}


def sales_start_checkout_node(state: AgentState) -> dict:
    product_query = state.get("product_query")
    catalog_backend = _get_catalog_backend()
    checkout_backend = _get_checkout_backend()
    if catalog_backend is None or checkout_backend is None:
        return {"sales_result": None}

    try:
        products = catalog_backend.search_products(product_query)
    except Exception as exc:
        print(f"  [NODE sales_start_checkout] catalog backend error: {exc!r}")
        return {"sales_result": None}

    # Resolver pra EXATAMENTE um produto antes de gerar qualquer link -
    # zero ou mais de um resultado não é um erro, é "não dá pra prosseguir
    # sem o cliente especificar melhor" (mesmo espírito de booking_failed
    # em scheduling_node.py, mas com dois motivos distintos em vez de um).
    # Lista cortada em 5 pra não inflar o prompt de generate_reply_node com
    # um catálogo inteiro - o suficiente pro cliente reconhecer qual quer.
    if len(products) == 0:
        return {"sales_result": {"checkout_failed": "not_found"}}
    if len(products) > 1:
        return {"sales_result": {"checkout_failed": "ambiguous", "candidates": products[:5]}}

    product = products[0]
    unit_amount_cents = _price_to_cents(product.get("price"))
    if unit_amount_cents is None:
        # Produto existe mas o catálogo não devolveu um preço utilizável -
        # gerar um link sem preço de verdade seria pior que recusar (um
        # link de R$ 0,00, ou pior, chutado, é exatamente o tipo de valor
        # inventado que este projeto trata como incidente, ver outputGuard.ts
        # do lado Node). Honesto > inventado, mesmo aqui sem o LLM.
        print(f"  [NODE sales_start_checkout] product {product.get('id')!r} has no usable price")
        return {"sales_result": {"checkout_failed": "invalid_price"}}

    # quantity vem como string (extração do LLM, ver intent_classifier.py) -
    # fallback pra 1 e nunca aceita um valor não-positivo (um "-3" ou "abc"
    # não é uma recusa de propósito, é ruído da extração).
    raw_quantity = state.get("quantity")
    try:
        quantity = int(raw_quantity) if raw_quantity else 1
        if quantity < 1:
            quantity = 1
    except (TypeError, ValueError):
        quantity = 1

    # reference_code gerado ANTES de chamar o backend, mesma ordem do
    # protocolo em book_appointment_node (scheduling_node.py) - só gera e
    # checa unicidade localmente, não grava nada ainda (ver generate_code()
    # em sale_store.py). Se create_checkout_link() falhar logo abaixo, o
    # código simplesmente nunca é persistido.
    sale_store = _get_sale_store()
    reference_code = sale_store.generate_code()

    order_summary = {
        "items": [{
            "name": product.get("name") or product.get("title") or product_query,
            "quantity": quantity,
            "unit_amount_cents": unit_amount_cents,
        }],
        "currency": "BRL",
        "customer": {"phone": state.get("requester_phone")},
        # Passthrough best-effort: PagSeguroCheckoutBackend ecoa isso de
        # volta como seu próprio reference_id (ver sales_payment_link.py);
        # StripeCheckoutBackend ignora este campo e devolve o id da sua
        # própria sessão. Em qualquer dos dois casos, o SaleStore local
        # continua sendo a fonte da verdade pra lookup (mesmo raciocínio do
        # preâmbulo de appointment_store.py) - isto é só uma cortesia extra
        # quando o provedor aceita.
        "reference_id": reference_code,
    }

    try:
        result = checkout_backend.create_checkout_link(order_summary)
    except Exception as exc:
        print(f"  [NODE sales_start_checkout] checkout backend error: {exc!r}")
        return {"sales_result": None}

    checkout_url = result.get("checkout_url")
    if not checkout_url:
        # Mesmo espírito de "backend respondeu mas não confirmou" de
        # booking_failed - o processador aceitou a chamada mas não devolveu
        # um link usável (ex.: PagSeguroCheckoutBackend não achou o link
        # "PAY" na resposta).
        return {"sales_result": {"checkout_failed": "not_found"}}

    sale_store.create(
        reference_code=reference_code,
        product_id=product.get("id"),
        quantity=quantity,
        customer=order_summary["customer"],
        checkout_url=checkout_url,
        provider_reference_id=result.get("reference_id"),
    )
    print(f"  [NODE sales_start_checkout] checkout link issued, reference_code={reference_code}")
    # Resposta final montada AQUI, sem chamar o LLM - mesmo principio do
    # ramo scheduling_link em book_appointment_node: não há nada pro modelo
    # decidir, a mensagem é sempre a mesma com o link trocado. route_after_booking
    # (scheduling_node.py, reaproveitado em graph.py) manda o grafo direto
    # pro END quando final_reply já existe.
    return {
        "sales_result": {**result, "reference_code": reference_code},
        "final_reply": message("checkout_link", state.get("language")).format(url=checkout_url),
    }
