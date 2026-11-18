# connectors/

Pluggable backends for four domain capabilities — `OrderBackend`,
`SchedulingBackend`, `CatalogBackend`, `CheckoutBackend` (`base.py`).
`graph.py` (and any future scheduling/sales graph) builds them via
`get_*_backend()` in `factory.py`, never by importing a concrete class
directly — swapping which system a client talks to is a `.env` change, not a
code change.

## OrderBackend — `ORDER_BACKEND`

| Value | Class | Required env vars |
|---|---|---|
| `rest` (default) | `RestOrderBackend` | `ORDER_API_BASE_URL` (falls back to `GATEWAY_BFF_URL` — this demo talks to the local GatewayBff by default, see `.env.example`) |

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
