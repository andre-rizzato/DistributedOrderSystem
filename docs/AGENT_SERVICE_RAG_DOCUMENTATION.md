# 🔎 AGENT SERVICE — ORDER HISTORY RAG AGENT

> Companion to `CHATBOT_SERVICE_DOCUMENTATION.md`. That document covers the
> C#/.NET bridge (`ChatbotService` → `AgentService`); this one covers what's
> actually inside `AgentService` (Python/FastAPI/LangGraph) — in particular
> the RAG pipeline added in 10/2026, which didn't exist before (the project's
> own roadmap had mapped an "Order History RAG Agent" since Week 10 of the
> course, but it was never built until now).

## Overview

`AgentService` is a standalone Python process (`uvicorn main:app --port 8100`)
exposing one real endpoint, `POST /agent/message`, consumed by
`ChatbotService`'s `PythonAgentChatbotService` bridge and, directly, by the
sibling Node product `ai-customer-service-agent`
(`src/orchestrator/agentServiceClient.ts`). Internally it's a compiled
**LangGraph** graph (`graph.py`): one intent-classification node, a pure
routing function, and a handful of worker nodes — two real
(`order_info_agent`, the new `retrieve_knowledge`), one security-hardened stub
that always ends in human handoff (`cancel_order_agent`), three honest stubs
(`create`/`update`/`product_info`), and one fallback (`clarify`).

## What's real vs. not

| Component | Real status |
|---|---|
| `classify_intent` (intent classification, few-shot + Instructor/Pydantic) | ✅ Real — calls Claude with a `response_model`, 6 possible `Literal` labels, 9 few-shot examples (2 added for the RAG work, see "Bugs found" below). |
| `order_info_agent` | ✅ Real — calls the configured `OrderBackend` (today: `RestOrderBackend` → GatewayBff) for a single order's live status. |
| `cancel_order_agent` | ✅ Real, but always ends in human handoff — the agent never calls a cancel endpoint itself (security review 2026-10-04, item #4). Routes straight to `END`, never through `generate_reply`. |
| `retrieve_knowledge` (Order History RAG Agent) | ✅ **New, 10/2026.** Full pipeline: HyDE → Voyage embeddings → Qdrant (filtered) → BM25 → RRF → Voyage rerank. Behind `ORDER_HISTORY_RAG_ENABLED` (default `false`) — see "Production posture" below. |
| `create_stub` / `update_stub` / `product_info_stub` | ❌ Honest stubs — no real backend endpoint exists yet; each returns a fixed "not available yet" message. |
| `generate_reply` | ✅ Real — the only node that calls the LLM for free-text generation, and only to *format* data already gathered by an earlier node (grounding principle, see "Design principles"). |
| RAGAS quality gate (`tests/rag_eval/`) | ✅ Real, not a demo — runs the actual compiled graph, 10/10 cases passing `Faithfulness ≥ 0.7` and `ContextPrecisionWithoutReference ≥ 0.5` as of this writing. Not run on every push (see `pytest.ini`'s `rag_eval` marker) — real Anthropic API cost + LLM-judge variance make it unsuitable as a hard gate on every commit. |

## Production posture — `ORDER_HISTORY_RAG_ENABLED`

Default `false`. The shared deploy VM (`vm-agente`, Standard B1s — 892MB RAM,
1 vCPU) already locked up once from two resident processes doing concurrent
`npm ci`; a third resident process (Qdrant) is a real resource risk there,
not a theoretical one. With the flag off, `retrieve_knowledge_node` returns
`{"retrieved_context": None}` immediately — the graph's shape never changes,
only this one node's behavior does, and `general_question` falls back to
exactly its pre-RAG behavior (`generate_reply` with no context). Qdrant
itself only runs locally today (`docker/qdrant/docker-compose.yml`) **and**
on a real, verified, low-cost Azure Container Apps instance (scale-to-zero,
0.5 vCPU/1GiB, Azure Files-backed) that `ai-customer-service-agent` also
reuses for its own catalog collection — standing up that infra does **not**
itself flip the flag; that's a separate, deliberately deferred decision.

## The graph (`graph.py`)

```
START
  └─> classify_intent  (intent_classifier.py — Instructor/Pydantic, 6 labels)
        └─[route_by_confidence: pure function, no model call]─┐
                                                                 confidence ≤ 0.70 ─> clarify ─> END
                                                      intent == cancel_order ─> cancel_order_agent ─> END
                                                      intent == create_order ─> create_stub ─> END
                                                      intent == update_order ─> update_stub ─> END
                              intent == get_info (no order_number) ─> product_info_stub ─> END
                                 intent == get_info (order_number) ─> order_info_agent ─┐
                                                      intent == general_question ─────┤
                                                   intent == order_history_query ─────┤──> retrieve_knowledge ─┐
                                                                                        │                        │
                                                                                        └────────────────────────┴─> generate_reply ─> END
```

`general_question` and `order_history_query` both land on the **same**
`retrieve_knowledge` node — the only difference between them is *which*
Qdrant collection to query and *whether* a privacy check applies, not the
shape of the work, so one node branches internally (`rag_node.py`) instead
of two near-duplicate nodes.

## The retrieval pipeline (`rag/`)

`retrieve_knowledge_node` (`rag_node.py`) is the only caller of
`rag/retrieval.py:retrieve()`; every other file under `rag/` is a building
block that `retrieve()` composes — nothing else in the graph calls them
directly.

```
retrieve(query, collection, requester_phone?, order_status?, issue_tag?)
  1. rag/hyde.py        generate_hypothetical_passage(query)     -> Claude Haiku, discardable
  2. rag/embeddings.py  embed_query(hypothetical_passage)        -> Voyage voyage-4-large
  3. rag/qdrant_client  get_qdrant_client().query_points(...)    -> filtered vector search, top 15
  4. rag/bm25.py        bm25_rank(query, candidate_texts)        -> keyword ranking, same pool
  5. rag/hybrid.py      reciprocal_rank_fusion([semantic, bm25]) -> combines the two rankings
  6. rag/reranker.py    rerank(query, fused_texts, top_k=3)      -> Voyage rerank-2
  -> [{text, score, payload}, ...]  (top 3)
```

For `order_history_query` specifically, `rag_node.py` also calls
`rag/filters.py:extract_retrieval_filters(message)` (a second, separate
Instructor call — structured `order_status`/`issue_tag` extraction, not
freeform judgment) **before** calling `retrieve()`, to build the Qdrant
filter — and refuses to call `retrieve()` at all if `requester_phone` is
absent (privacy guardrail, see below).

Two Qdrant collections, seeded via `rag/ingest.py` from
`rag/seed_data/*.jsonl`:

| Collection | Records | Payload fields | Filtered by |
|---|---|---|---|
| `order_support_notes` | 45 synthetic | `order_id`, `customer_phone`, `order_status`, `issue_tag`, `note_text`, `created_at` | `customer_phone` (always, when queried) + optional `order_status`/`issue_tag` |
| `faq_policy` | 18 synthetic | `doc_id`, `category`, `note_text` | none — public content |

## Design principles (carried over from Weeks 6–16 of the course)

- **Deterministic flow, LLM only at the edges.** HyDE's LLM call produces a
  discardable embedding aid, never shown to the customer and never treated
  as fact. `extract_retrieval_filters` is structured Instructor output, not
  freeform judgment. Qdrant filtering, BM25, RRF, and reranking are all
  plain, inspectable code. `generate_reply_node` is the *only* place a model
  decides what text the customer sees, and even there it's instructed to
  ground every sentence in context already gathered — never to add
  unstated advice (a real RAGAS-caught failure, fixed mid-session: the model
  once added "you may want to contact your bank" with nothing in context
  supporting it).
- **Privacy guardrail, not an afterthought.** `order_history_query` requires
  a verified `requester_phone`. Without one (true for every Telegram
  session today — a chat id isn't a verified phone number), the node
  refuses to query at all, rather than querying with no filter. Verified
  empirically while building this: querying `order_support_notes` with no
  `customer_phone` filter returns *other* customers' notes — the risk is
  real, not theoretical.
- **Cancellation never executes inside the agent.** `cancel_order_agent`
  routes straight to `END`, bypassing `generate_reply` entirely — there is
  nothing for a model to decide, the outcome (human handoff) is always the
  same fixed message.

## Bugs found and fixed during this build (real, RAGAS/live-testing-caught — not hypothetical)

1. **`ragas` 0.4.3** unconditionally imports a module dropped by
   `langchain-community>=0.4.0` → pinned `langchain-community<0.4.0`.
2. **`ragas`'s `InstructorModelArgs`** sends `temperature`/`top_p` that the
   resolved `anthropic==1.12.0` SDK no longer accepts on `messages.create()`
   → popped both from `model_args` after `llm_factory(...)`.
3. **`qdrant-client`'s `port` parameter defaults to 6333** and gets appended
   to the host even when `url` is a full HTTPS URL with no port in the
   string — broke every hosted Qdrant connection until `port=None` was
   passed explicitly (`rag/qdrant_client.py`).
4. **Intent misclassification, twice, in sequence** — "why is my order
   delayed?" first misrouted to `get_info`; a necessary fix then
   over-corrected and misrouted a *policy* question ("refund for an order
   cancelled over a month after delivery") INTO `order_history_query`,
   wrongly triggering the identity guardrail. Fixed with a sharper `Field`
   description (the "actually happened" vs. "policy/hypothetical" test)
   plus two contrastive few-shot examples.
5. **`extract_retrieval_filters` over-inferred `order_status=Delivered`**
   from "my item arrived broken" — the real matching record's status was
   actually `Cancelled`. Fixed by forbidding inference from indirect event
   descriptions in the field's description, plus a new few-shot example
   using the exact failing phrase.
6. **`generate_reply_node`'s system prompt hardcoded "Reply in English"**
   (pre-existing, not introduced by this build) — caught live, not by
   RAGAS: it stayed mostly invisible before this build, since few messages
   reached `generate_reply_node` with zero context to begin with. Once
   `order_history_query` started routing "status do pedido"-style messages
   here (privacy guardrail above, no `requester_phone` on Telegram), a
   Portuguese-speaking customer got an English reply — reported from the
   sibling Node product's own live test on 07/10/2026. Fixed by adding
   `AgentRequest.language` (sent by `agentServiceClient.ts`, same value
   `ai-customer-service-agent`'s own `promptBuilder.ts` already resolves per
   channel) threaded through `AgentState.language`, and making every reply
   path language-aware: `generate_reply_node`'s system prompt picks the
   instruction from `language` (falls back to "match the customer's
   message" when absent, e.g. WhatsApp today); the nodes that never call the
   LLM at all (`cancel_order_agent`, `create/update/product_info_stub`,
   `clarify`) now pull their fixed string from the new `messages.py`
   (pt/en/it, mirrors the Node product's `messages.ts`, defaults to `pt`
   when `language` is absent/unknown). `test_contract_agent_message.py`'s
   cancel-order contract test now pins `language: "en"` explicitly, since
   the key phrase it locks in only shows up in the English copy.

## Testing

- `tests/test_contract_agent_message.py` — contract tests (no `rag_eval`
  marker, runs on every push): `cancel_order` reply/intent unchanged,
  response schema never leaks `retrieved_context`, an order-history question
  with no `requester_phone` never 500s.
- `tests/rag_eval/` — RAGAS quality gate, `pytest -m rag_eval`, manual/CI
  `workflow_dispatch` only (see `.github/workflows/rag-eval.yml`), not on
  every push.

## Related docs

- `src/AgentService/rag/README.md` — module-level documentation (pipeline
  diagram, "where Qdrant runs" table, the `port=None` finding in full).
- `docs/CHATBOT_SERVICE_DOCUMENTATION.md` — the C#/.NET side of the bridge.
- `ai-customer-service-agent/eval/README.md` (sibling repo) — the
  TypeScript port of this same RAG technique, and its own RAGAS harness.
