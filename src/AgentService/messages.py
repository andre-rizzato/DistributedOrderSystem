# src/AgentService/messages.py
#
# Fixed (non-LLM) messages the agent sends, per supported language -
# mirrors the Node orchestrator's src/orchestrator/messages.ts (same
# product, same pt/en/it set, same "undefined/unknown language falls
# back to pt" rule). Created 07/10/2026 alongside the language bug fix in
# graph.py's generate_reply_node: those nodes (cancel_order_agent,
# create/update/product_info stubs, clarify) never call the LLM at all,
# so there's no prompt to make language-aware - they returned a literal
# English string regardless of state["language"].
#
# For cancel_order_agent_node specifically, the Node orchestrator already
# overrides AgentResponse.reply with its own localized copy before it
# ever reaches a Telegram/WhatsApp customer (orchestrator.ts, intent ==
# "cancel_order" branch) - but main.py's /agent/message is also called
# directly by the C# ChatbotService (see main.py's top comment), which
# has no such override. These strings need to be right regardless of
# which caller is listening.
from typing import Optional

DEFAULT_LANGUAGE = "pt"

MESSAGES: dict[str, dict[str, str]] = {
    "cancel_order_handoff": {
        "pt": "Entendi que você quer cancelar um pedido — para garantir que isso seja "
        "feito corretamente e com segurança, vou te conectar com um atendente humano "
        "que vai confirmar o cancelamento com você diretamente.",
        "en": "I understand you'd like to cancel this order. To make sure this is "
        "handled correctly and securely, I'm connecting you with a human agent who "
        "will confirm the cancellation with you directly.",
        "it": "Ho capito che vuoi annullare questo ordine. Per assicurarmi che venga "
        "gestito correttamente e in sicurezza, ti metto in contatto con un operatore "
        "che confermerà l'annullamento direttamente con te.",
    },
    "create_stub": {
        "pt": "Fazer pedidos pelo assistente ainda não está disponível — finalize sua "
        "compra no site, por favor.",
        "en": "Placing orders through the assistant isn't available yet - please "
        "complete your purchase on the site.",
        "it": "Effettuare ordini tramite l'assistente non è ancora disponibile - "
        "completa l'acquisto sul sito, per favore.",
    },
    "update_stub": {
        "pt": "Alterar pedidos pelo assistente ainda não está disponível — contate o "
        "atendimento humano, por favor.",
        "en": "Changing orders through the assistant isn't available yet - please "
        "contact human support.",
        "it": "Modificare gli ordini tramite l'assistente non è ancora disponibile - "
        "contatta l'assistenza umana, per favore.",
    },
    "product_info_stub": {
        "pt": "Consultar produtos pelo assistente ainda não está disponível — confira "
        "o catálogo no site, por favor.",
        "en": "Looking up products through the assistant isn't available yet - "
        "please check the catalog on the site.",
        "it": "Consultare i prodotti tramite l'assistente non è ancora disponibile - "
        "controlla il catalogo sul sito, per favore.",
    },
    "clarify": {
        "pt": "Não tenho certeza se entendi — você pode reformular ou dar mais "
        "detalhes sobre o que precisa?",
        "en": "I'm not sure I understood - could you rephrase or give more detail "
        "about what you need?",
        "it": "Non sono sicuro di aver capito - puoi riformulare o darmi più dettagli "
        "su cosa ti serve?",
    },
}


# Preâmbulo: message() devolve o texto fixo no idioma pedido, caindo em
# DEFAULT_LANGUAGE se `language` vier None/desconhecido - mesmo
# comportamento de messages.ts (uma mensagem em português é melhor do que
# o campo vir vazio pro cliente).
def message(key: str, language: Optional[str]) -> str:
    lang = language if language in ("pt", "en", "it") else DEFAULT_LANGUAGE
    return MESSAGES[key].get(lang, MESSAGES[key][DEFAULT_LANGUAGE])
