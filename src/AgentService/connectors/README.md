# connectors/

Pluggable backends for the four domain capabilities from the "Mapa de
Capacidades" design doc (lives in the sibling `ai-customer-service-agent`
repo at `docs/artifacts/mapa-capacidades.html`) — `OrderBackend`,
`SchedulingBackend`, `CatalogBackend`, `CheckoutBackend` (`base.py`).
Full done/pending checklist across both repos: `ai-customer-service-agent`'s
`docs/STATUS.md`.
`graph.py` (and any future scheduling/sales graph) builds them via
`get_*_backend()` in `factory.py`, never by importing a concrete class
directly — swapping which system a client talks to is a `.env` change, not a
code change.

## OrderBackend — `ORDER_BACKEND`

| Value | Class | Required env vars |
|---|---|---|
| `rest` (default) | `RestOrderBackend` | `ORDER_API_BASE_URL` (falls back to `GATEWAY_BFF_URL` for the DistributedOrderSystem demo client) |

Test infra (`rg-agente-atendimento`, see the Node product's `docs/GO_LIVE_CHECKLIST.md`) has no reachable `GatewayBff` yet — `DistributedOrderSystem` only runs locally. Until it has a real reachable environment, that VM's `agent-service/.env` points `ORDER_API_BASE_URL` at `https://dummyjson.com` with `ORDER_API_GET_PATH=/carts/{id}` and `ORDER_API_CANCEL_PATH=/carts/{id}` + `ORDER_API_CANCEL_METHOD=DELETE` — a public, no-auth mock that returns realistic cart/order-shaped JSON, close enough to exercise the full pipeline (intent → connector → grounded reply) for real. Swap back to `GATEWAY_BFF_URL`/a real `ORDER_API_BASE_URL` once a staging/production environment exists (see below).

Optional: `ORDER_API_GET_PATH` (default `/api/queries/orders/{id}`),
`ORDER_API_CANCEL_PATH` (default `/api/commands/orders/cancel/{id}`),
`ORDER_API_CANCEL_METHOD` (default `PUT`), `ORDER_API_AUTH_HEADER` +
`ORDER_API_AUTH_VALUE` (if the client's API needs an API key header).

## CatalogBackend — `CATALOG_BACKEND`

| Value | Class | Required env vars |
|---|---|---|
| `rest` (default) | `RestCatalogBackend` | `CATALOG_API_BASE_URL` (falls back to `GATEWAY_BFF_URL`) |

Optional: `CATALOG_API_SEARCH_PATH`, `CATALOG_API_GET_PATH`,
`CATALOG_API_AUTH_HEADER` + `CATALOG_API_AUTH_VALUE`.

## CheckoutBackend — `CHECKOUT_PROCESSOR`

| Value | Class | Required env vars |
|---|---|---|
| `pagseguro` (default) | `PagSeguroCheckoutBackend` | `PAGSEGURO_TOKEN`. Optional: `PAGSEGURO_NOTIFICATION_URL`, `PAGSEGURO_SANDBOX=true` |
| `stripe` | `StripeCheckoutBackend` | `STRIPE_SECRET_KEY`, `STRIPE_SUCCESS_URL`, `STRIPE_CANCEL_URL` |

Pick `pagseguro` for a client selling to Brazil (card + Pix in one checkout);
`stripe` for a client selling internationally. See "Decisão · pagamento" in
the design doc for why.

**Verify `PagSeguroCheckoutBackend`'s endpoint/payload against
[dev.pagbank.com.br](https://dev.pagbank.com.br) before pointing it at
production** — it was written from the documented API shape, not tested
against a live PagSeguro account in this session. `StripeCheckoutBackend`
uses Stripe's long-stable Checkout Sessions API and needs no such caveat.

## SchedulingBackend — `SCHEDULING_BACKEND`

| Value | Class | Required env vars |
|---|---|---|
| `google_calendar` (default) | `GoogleCalendarBackend` | `GOOGLE_SERVICE_ACCOUNT_FILE` (path to the service-account JSON key, shared with that calendar), `GOOGLE_CALENDAR_ID`, `SCHEDULING_SERVICES` (e.g. `corte:30,coloracao:90` — service id to duration in minutes) |

Optional: `SCHEDULING_BUSINESS_HOUR_START`/`_END` (default `9`/`18`),
`SCHEDULING_SLOT_MINUTES` (default `30`), `SCHEDULING_TIMEZONE` (default
`America/Sao_Paulo`).

Not exercised against a live Google Calendar in this session (no test
service account available) — the slot-picking logic itself
(`compute_available_slots`) has unit coverage in `tests/test_scheduling_slots.py`
with no network involved; the HTTP calls follow the documented Calendar API
v3 contract but should be checked against a real calendar before a client
depends on them.

## No generic CustomBackend

A client with neither a REST API, Google Calendar, nor a supported payment
processor doesn't get a one-size class — see `custom.py` for the skeletons
to copy per capability. Prefer asking the client to put a thin API in front
of their own system over querying it directly; a custom connector that skips
that is exactly the fragile coupling the generic connectors above exist to
avoid.

## Processo na VM (PM2) — bind em 127.0.0.1, não 0.0.0.0

Revisão de segurança de 04/10/2026 (item #6, ver `ai-customer-service-agent`'s
`docs/SECURITY_REVIEW.md`): até essa data, o processo `agent-service` do PM2
na VM `vm-agente` rodava com `--host 0.0.0.0`, expondo a porta 8100
diretamente pra internet pública (a única proteção era o NSG do Azure — uma
única camada, sem defesa em profundidade). O Node (`agente-atendimento`) e o
`agent-service` sempre rodaram na MESMA VM e o Node já fala com ele via
`AGENT_SERVICE_URL=http://localhost:8100` (ver `.env` do Node) — não existe
nenhum motivo real pra essa porta ser alcançável de fora da própria VM, então
o bind correto é só em loopback.

Comando usado para (re)criar o processo do zero na VM (não existe um
`ecosystem.config.js` neste repo — o processo foi criado direto via `pm2
start`, documentado aqui pra ser reproduzível sem precisar reconstruir esse
comando por tentativa e erro de novo):

```bash
cd /home/azureuser/agent-service
pm2 start .venv/bin/uvicorn --name agent-service --cwd /home/azureuser/agent-service --interpreter none -- main:app --host 127.0.0.1 --port 8100
pm2 save
```

`--interpreter none` é obrigatório — sem ele, o PM2 tenta rodar o binário do
uvicorn (um script Python com shebang) através do interpretador Node.js
embutido e falha com `SyntaxError: Invalid or unexpected token` logo na
primeira linha do arquivo (`# -*- coding: utf-8 -*-`), entrando em loop de
restart. Esse é o mesmo valor que o processo já tinha antes dessa mudança
(confirmado via `pm2 describe agent-service` antes de recriá-lo) — só o
`--host` mudou de `0.0.0.0` pra `127.0.0.1`.

O deploy automático (`.github/workflows/deploy-agent-service.yml`) só roda
`pm2 restart agent-service --update-env` — ele NUNCA re-especifica
`--host`/`--interpreter`, porque só reinicia um processo que o PM2 já
conhece (flags ficam gravadas no `dump.pm2` salvo por `pm2 save`). Se esse
processo for deletado por qualquer motivo no futuro (`pm2 delete`, VM
recriada do zero), ele precisa ser recriado com o comando acima — não com o
`--host 0.0.0.0` antigo.

## Environments

`rg-agente-atendimento` on Azure is a **test environment only** — one VM,
one tenant, used to validate that the whole pipeline (Node → AgentService →
connector → grounded reply) actually works. Staging and production (likely
per-client, see "Implantação multi-tenant" in the Node product's
`docs/artifacts/mapa-capacidades.html`) get built when there's a first real
client to deploy for — not before. The dummyjson.com fallback above is
scoped to today's test VM, not a signal to build more environment
infrastructure now.
