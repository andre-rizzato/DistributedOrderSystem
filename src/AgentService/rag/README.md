# rag/

The Order History RAG Agent's retrieval pipeline - the piece mapped in the
project's multi-agent architecture since Week 10 but never built until now.
Gives `graph.py`'s `retrieve_knowledge_node` (`rag_node.py`, one level up)
real semantic search over two corpora instead of the "no specific order
data available" fallback `general_question` used to hit unconditionally.

## Pipeline

```
HyDE (hyde.py) -> embed (embeddings.py) -> Qdrant filtered search (qdrant_client.py)
              -> BM25 (bm25.py) -> RRF (hybrid.py) -> rerank (reranker.py) -> top-N
```

Same techniques, same reasoning, as `CursoClaude`'s week11-16 exercises -
this is that mechanism applied to the real order-support domain, not a
toy corpus. `retrieval.py` is the one function `rag_node.py` calls;
every other file here is a building block it composes.

## Collections

| Collection | What it holds | Filterable by | Needs identity? |
|---|---|---|---|
| `order_support_notes` | Synthetic support notes, one per (order, issue) | `customer_phone`, `order_status`, `issue_tag` | Yes - `rag_node.py` refuses to query this collection at all without a verified `requester_phone` |
| `faq_policy` | Public policy/FAQ clauses | none | No |

Seed data: `seed_data/*.jsonl`, loaded by `ingest.py` (`python -m rag.ingest`,
run from `src/AgentService/`). Re-run whenever the seed corpus changes - it's
a full replace (`client.upsert` on fixed integer ids), not incremental.

## Feature flag

`ORDER_HISTORY_RAG_ENABLED` (`config.py`, default `false`) is the
production kill switch. Off: `retrieve_knowledge_node` returns immediately
with no context, same shape as the old `general_question -> generate_reply`
path. On: the full pipeline above runs. See `config.py`'s top comment for
why this exists (the shared deploy VM's RAM ceiling) - standing up the
hosted Qdrant (below) does NOT itself turn this on.

## Where Qdrant runs

| Environment | `QDRANT_URL` | Auth |
|---|---|---|
| Local dev | `http://localhost:6333` (default) - `docker/qdrant/docker-compose.yml` | none |
| Production | Azure Container Apps (`ca-qdrant`, `rg-agente-atendimento`, `eastus2`) - via Key Vault (`qdrant-url` secret) | `qdrant-api-key` secret, same Key Vault |

The hosted instance scales to zero (`minReplicas: 0`) when idle - cost is
near-zero while `ORDER_HISTORY_RAG_ENABLED` stays off, since nothing ever
queries it. Persistent storage: an Azure Files share (`qdrant-storage`,
`stagenteatendimento` storage account), not ephemeral container disk.

`qdrant-client` quirk found standing this up: its `port` parameter defaults
to `6333` and gets appended to the host even when `url` is already a full
`https://` URL with no port of its own - see `qdrant_client.py`'s
`port=None` comment. Harmless locally (`http://localhost:6333` already
embeds its port in the string), broke every request against the hosted
instance (only reachable on the standard HTTPS port) until fixed.

## CI

`tests/rag_eval/` (RAGAS quality tests) run against the HOSTED Qdrant, not
a throwaway one, via `.github/workflows/rag-eval.yml` - authenticated to
`kv-agente-atendimento` through an Azure AD federated credential (OIDC),
not a stored GitHub Secret. See that workflow's top comment for the full
reasoning. `pytest -m rag_eval` to run locally; excluded by default
(`-m "not rag_eval"`) from both the local default run and
`deploy-agent-service.yml`'s push-triggered one - real API cost and
LLM-judge variance make it unsuitable as a hard gate on every push.

## Known rough edge

With `ORDER_HISTORY_RAG_ENABLED=false`, an `order_history_query` message
still gets the "I can't look up your order history without verifying your
identity" reply from `generate_reply_node` even when `requester_phone`
*was* present - the flag-off path and the actual privacy-guardrail-fired
path both leave `retrieved_context` as `None`, and `generate_reply_node`
can't currently tell them apart. Not a safety issue (nothing leaks either
way), just a slightly misleading reason given to the customer while the
flag is off. Worth a distinct state value if this flag stays off for a
while and the message starts getting reported as confusing.
