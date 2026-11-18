# src/AgentService/sales_guard.py
# Checagem determinística de preço pra respostas da capacidade de vendas
# (2026-10-20): uma regra de prompt pedindo pro modelo "nunca inventar
# preço" é um pedido, não uma garantia.
#
# POR QUE ISTO NÃO É SÓ TEXTO DE CATÁLOGO: a fonte de verdade de preço aqui
# não é um documento de catálogo estático, é a própria estrutura de dados
# (sales_result) que gerou o contexto do LLM - os dados REAIS que voltaram
# do backend específico desta chamada (CatalogBackend/CheckoutBackend).
#
# LIMITAÇÃO CONHECIDA (documentada, não um bug): compara contra QUALQUER
# número presente na estrutura de dados, não só campos de preço - um valor
# que coincida por acaso com, por exemplo, uma quantidade em estoque,
# passaria. Aceito porque o objetivo é bloquear valores claramente
# inventados (R$ 50 de desconto que não existe em lugar nenhum), não
# validar cada dígito com precisão perfeita.
#
# SEM RETRY: não há como re-prompt o mesmo generate_reply_node com uma
# instrução corretiva específica sem duplicar aquela função inteira aqui.
# Uma falha = mensagem fixa em vez de risco, sempre - ver graph.py's
# generate_reply_node, bloco de vendas.

import re

# Símbolo de moeda obrigatório, senão "6x" ou "12 meses" virariam "valores".
_MONEY_RE = re.compile(
    r"(?:R\$|US\$|BRL|€|\$)\s?(\d{1,3}(?:[.,\s]\d{3})*(?:[.,]\d{1,2})?|\d+(?:[.,]\d{1,2})?)"
)
_CENTS_TOLERANCE = 0.011


def _parse_money_number(raw: str) -> float:
    clean = raw.replace(" ", "")
    # Se houver separador seguido de 1-2 dígitos no FIM, é o decimal; o
    # resto é milhar.
    m = re.match(r"^(.*?)[.,](\d{1,2})$", clean)
    if m:
        return float(m.group(1).replace(".", "").replace(",", "") + "." + m.group(2))
    return float(clean.replace(".", "").replace(",", ""))


def _extract_money(text: str) -> list[float]:
    return [_parse_money_number(m.group(1)) for m in _MONEY_RE.finditer(text)]


def _extract_reference_numbers(data) -> list[float]:
    # Percorre recursivamente a estrutura (lista de produtos, ou dict de
    # candidatos/checkout) coletando todo número - ver "LIMITAÇÃO
    # CONHECIDA" no preâmbulo do arquivo sobre por que não filtra só campos
    # de preço.
    numbers: list[float] = []

    def walk(node) -> None:
        if isinstance(node, dict):
            for value in node.values():
                walk(value)
        elif isinstance(node, list):
            for value in node:
                walk(value)
        elif isinstance(node, (int, float)) and not isinstance(node, bool):
            numbers.append(float(node))

    walk(data)
    return numbers


# Preâmbulo: verify_sales_reply_prices() é chamada por generate_reply_node
# (graph.py) depois de gerar a resposta final pra um dos dois intents de
# vendas. Devolve a lista de valores em dinheiro citados na resposta que
# NÃO aparecem nos dados reais desta mesma consulta - lista vazia significa
# aprovado. Com `reference_data` vazio/None, qualquer valor citado é
# suspeito.
def verify_sales_reply_prices(reply: str, reference_data) -> list[str]:
    reference_numbers = _extract_reference_numbers(reference_data)
    unsupported: list[str] = []
    for value in _extract_money(reply):
        if not any(abs(value - ref) <= _CENTS_TOLERANCE for ref in reference_numbers):
            unsupported.append(f"{value:.2f}")
    return unsupported
