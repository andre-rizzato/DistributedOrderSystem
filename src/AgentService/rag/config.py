# src/AgentService/rag/config.py
#
# Centralizes every environment-driven decision the RAG module needs, so no
# other file in rag/ ever calls os.getenv() directly - one place to look
# when wiring a new environment, one place to change if a variable name
# ever needs to move.
#
# The single most important thing in this file is ORDER_HISTORY_RAG_ENABLED:
# it is the production kill switch. The shared deploy VM (vm-agente, a B1s -
# 892MB RAM, 1 vCPU) already locked up once from the two services it runs
# today doing concurrent installs; a third resident process (Qdrant) is a
# real resource risk there, not a theoretical one. Until a deliberate
# hosting decision is made (Qdrant Cloud free tier, a dedicated small VM,
# or something else), production keeps this flag OFF - the graph falls
# back to its current behavior (general_question -> generate_reply with no
# context) exactly as it does today, see graph.py's conditional edges.

import os

# True only when the env var is the literal string "true" (case-insensitive) -
# anything else (unset, "false", "0", a typo) means OFF. Defaulting to
# "false" is the safe choice: a missing/misspelled env var should never
# silently turn production retrieval on.
ORDER_HISTORY_RAG_ENABLED: bool = os.getenv("ORDER_HISTORY_RAG_ENABLED", "false").lower() == "true"

# Qdrant connection. QDRANT_URL defaults to the local Docker container from
# docker/qdrant/docker-compose.yml - production overrides this via env var
# once a hosting decision is made. QDRANT_API_KEY is None for the local,
# unauthenticated container; a hosted Qdrant (e.g. Qdrant Cloud) requires it.
QDRANT_URL: str = os.getenv("QDRANT_URL", "http://localhost:6333")
QDRANT_API_KEY: str | None = os.getenv("QDRANT_API_KEY") or None

# Voyage AI embedding model - same choice as the course (voyage-4-large for
# indexing/querying; see CursoClaude's week11-16 for why this model and not
# a local/open-weight one at this stage of the project).
VOYAGE_EMBEDDING_MODEL: str = "voyage-4-large"
VOYAGE_RERANK_MODEL: str = "rerank-2"

# Qdrant collection names - the two corpora described in the implementation
# plan: order-specific support notes (requires requester_phone, see
# rag_node.py's privacy guardrail) and public FAQ/policy clauses (no
# identity check needed).
COLLECTION_ORDER_SUPPORT_NOTES: str = "order_support_notes"
COLLECTION_FAQ_POLICY: str = "faq_policy"
