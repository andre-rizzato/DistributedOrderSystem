# Complete System Documentation - Summary

> Updated 2026-09-04 to reflect the actual state of the solution. Every document in this folder has now been checked against the real source code (not just this index) — several went well beyond stale ports/DB names into genuinely dead code and functional bugs. See the table below for specifics per document, and the "Actual Implementation Status" section for the corrected picture of what's real, what's mocked, and what's dead.

For the always-current overall architecture, see **`COMPREHENSIVE_SOLUTION_ARCHITECTURE_EN.md`** at the repository root (not in `docs/`).

---

## 📚 Documents in `docs/`

| File | Topic | Status vs. current code |
|---|---|---|
| `ARCHITECTURE_OVERVIEW.md` | Detailed architecture guide, service by service | ✅ Rewritten: replaces the old "Inventory/Payment/etc. all remain simple N-tier" blanket claim with an accurate per-service breakdown. Also documents GatewayBff's cart/wishlist inconsistency and adds the new `AppHost` |
| `BFF_ARCHITECTURE.md` | How CustomerWebsite routes cart/wishlist through GatewayBff | ✅ Rewritten: cart/wishlist genuinely go through GatewayBff (which calls CustomerService via a hardcoded `localhost:5009` URL), while products/orders in CustomerWebsite call ProductService/OrderService directly. Documents the port mismatch in `CustomerWebsite/appsettings.json` |
| `CHATBOT_SERVICE_DOCUMENTATION.md` | ChatbotService documentation (NLP, intents) | ✅ Rewritten. **Major finding**: the entire `/api/chat/*` chat API is dead code (`IChatbotService` has no implementation anywhere, its DI registration is commented out). The "ONNX DialoGPT" NLP engine is **fully simulated** — the model files on disk are tiny placeholder text files the service wrote itself, `InferenceSession` is never instantiated, and replies come from keyword matching against canned Italian templates. Auth uses hardcoded demo credentials. Fine-tuning is simulated client-side. The `/` → `/api/admin` redirect is itself a 404 (routing mismatch) |
| `CHATBOT_WIDGET_DOCUMENTATION.md` | Embeddable chatbot JS widget | ✅ Rewritten, scope narrowed to the widget itself. The widget is a real, substantial artifact (864-line TS source, working static-file endpoints) — but neither of its two routing modes (BFF / Direct) can complete an actual chat today, because both ultimately hit the dead chat API described above |
| `INVENTORY_SERVICE_DOCUMENTATION.md` | InventoryService: data schema, cache, Kafka consumer | ✅ Rewritten: `ProductId` is `Guid` (not `int`), DB is PostgreSQL. Found real quirks: `AdjustInventoryQuantityAsync` rejects bringing stock exactly to 0 (`<= 0` check), Redis cache TTL is hardcoded to 5 minutes (several `appsettings` Redis keys are dead config), insufficient-stock during Kafka consumption is logged but still commits the offset (no retry — only technical exceptions block commit) |
| `KAFKA_INTEGRATION.md` | Producer/consumer architecture, event flow | ✅ Rewritten. Added the dual-listener explanation (`9092` container-internal / `29092` host-facing). Corrected: producer uses `Acks.All` not `Acks.Leader`; `OrderCreatedEvent`'s `OrderId`/`ProductId` are `string`, not `int`; most quoted "log lines to watch for" were invented English text — the real logs are mostly in Italian, now quoted verbatim |
| `KAFKA_TESTING_GUIDE.md` | Practical Kafka testing guide with curl | ✅ Rewritten: SQL Server verification commands replaced with PostgreSQL/`psql`, product ids in curl examples corrected from `int` to `Guid`, noted PaymentService has Kafka settings scaffolded but zero implementation |
| `NOTIFICATION_SERVICE_DOCUMENTATION.md` | Multi-channel NotificationService (SMS/Email/Push/In-app) | ✅ Rewritten. The service is real (PostgreSQL + Hangfire + SignalR, mock/real channel split by environment) but several features are broken or dead: push token registration (`RegisterDeviceTokenAsync`) queries a `UserDeviceTokens` table that doesn't exist in the EF model — throws outside Development; retry/backoff/rate-limit/template-cache settings are bound in DI but never read anywhere; `/health` always reports healthy because zero checks are actually registered; the Hangfire dashboard's authorization filter always returns `true` in every environment; confirmed no Kafka usage anywhere in this service |
| `ORDER_SERVICE_IMPLEMENTATION.md` | OrderService implementation summary | ✅ Rewritten: replaced the old flat `Models/Order.cs` + `OrderWorkerService` description (no longer exists) with the actual DDD structure. Also found: `MockOrderEventProducer` is fully implemented but never registered in `Program.cs` — OrderService requires a live Kafka broker in every environment, with no mock fallback (unlike NotificationService's env-gated mocks) |
| `PAYMENT_AND_NOTIFICATION_SERVICES_DESIGN.md` | Guided design for PaymentService and NotificationService | ✅ Annotated: the PaymentService section is now unambiguous — **100% unimplemented**, no Controllers/Models/Services/Data folders exist at all. The NotificationService section is kept as historical design context, but flagged wherever it diverges from the real implementation (the design proposes Kafka consumers and SMTP/SendGrid; the real service has neither — no Kafka reference in the project at all, and uses Twilio/MailKit/Firebase instead) |
| `PRODUCT_SERVICE_DOCUMENTATION.md` | ProductService: patterns, Redis cache, API | ✅ Rewritten: Clean Architecture + CQRS via MediatR confirmed, `Product.Id` is `Guid`, DB is PostgreSQL. Found real gaps: `SearchProductsQuery` silently ignores its `Category` filter and returns unfiltered results; `InMemoryProductCache` exists but is never registered (Redis is always required, no fallback); ~9 "e-commerce" endpoints (`featured`, `bestsellers`, `categories`, `brands`, reviews) are hardcoded placeholders disconnected from real data — reviews aren't persisted anywhere; the Redis cache key format and TTL don't match what the old doc described (TTL is hardcoded to 5 minutes, ignoring config) |
| `SHOPVERSE_PLATFORM_DOCUMENTATION.md` | Documentation for the "ShopVerse" platform (CustomerWebsite) | ✅ Rewritten. **Major finding**: most of the referenced `.csproj` packages are dead code with zero usage in source — Stripe.net, Serilog, AutoMapper, FluentValidation, SignalR.Client, ImageSharp.Web are all referenced but unused. No `DbContext` despite the SqlServer package reference. Session cart uses in-memory storage, not the referenced Redis package. **Most Views don't exist** — `Product/Details`, `Cart/Index`, `Checkout/Index`, `Search`, `Categories` have working controller actions but no `.cshtml`, so they 500 at runtime; only the homepage is actually navigable. Wishlist/checkout actions generate a throwaway `Guid.NewGuid()` as the user id on every call, so a user's own wishlist/orders can never be looked up again |
| `STARTUP.md` | Quick-start guide | ✅ Rewritten: `.NET 8` → `.NET 9` (AppHost needs .NET 10), SQL Server → PostgreSQL (container name and `psql` test command corrected), `/swagger` links → `/scalar/v1` (this solution uses Scalar, not Swagger UI, on these services). Added a section listing the five services this guide's script doesn't start, plus the AppHost alternative |
| `USER_SERVICE_DOCUMENTATION.md` | UserService: authentication, Identity, JWT, OAuth | ✅ Rewritten: DB is PostgreSQL, Identity/JWT/OAuth confirmed real and working. Found real bugs: email sending silently fails in every environment (`CommunicationService` defaults to UserService's own port, and even redirected to NotificationService the route path doesn't match — all failures are caught and only logged); the password-reset link points at a frontend route no frontend in this repo implements; CORS only allows stale ports (`:3000`/`:7000`/`:7001`), none of which match the real Angular/GatewayBff/CustomerWebsite ports; GatewayBff has zero integration with UserService (no JWT middleware, no HttpClient). Also found the payment-method AES encryption reuses a static IV — a real crypto weakness, not just a "harden before production" note |
| `VIEWCOMPONENT_FLOW_GUIDE.md` | Using Razor ViewComponents in CustomerWebsite (e.g. ProductCard) | ✅ Rewritten: the real `ProductCardViewComponent.Invoke` is synchronous with no dependency injection (the old doc's async/DI example never existed in code); real invocation is the `<vc:product-card>` Tag Helper, not `@await Component.InvokeAsync(...)`; the "reused on Search/Category/Wishlist pages" claim was aspirational — those views don't exist. The homepage falls back to 8 hardcoded placeholder products (fresh GUIDs per render) when the backend returns nothing |
| `DOCUMENTATION_SUMMARY.md` | This file | — |

**Outside `docs/`**: `COMPREHENSIVE_SOLUTION_ARCHITECTURE_EN.md` at the repository root is the main, most up-to-date architecture document.

---

## 🎯 Actual Implementation Status

Services below are grouped by how much of what they claim to do is actually real and reachable — not just "does a folder structure exist."

### ✅ Solid — core functionality works as described

1. **OrderService** (port 5003) — DDD reference implementation: `Order` aggregate, `Money`/`OrderStatus` value objects with enforced state transitions, Kafka producer (`OrderEventProducer`, `Acks.All`), PostgreSQL. A Kafka publish failure doesn't block order creation. Caveat: no mock producer is wired up anywhere, so this service hard-requires a live Kafka broker even in Development.

2. **InventoryService** (port 5051) — Kafka consumer (`OrderCreatedConsumer`, manual offset commit), Redis cache-aside, PostgreSQL. Caveats: `AdjustInventoryQuantityAsync` can't reduce stock to exactly 0; insufficient-stock during consumption doesn't trigger a retry (offset still commits).

3. **CustomerService** (port 5009) — Redis only, no database: cart and wishlist are ephemeral by design. The simplest, most honestly-scoped service in the solution.

4. **GatewayBff** (port 5189) — Custom MediatR-based BFF: catalog aggregation (Product + Inventory) and order/product/inventory commands via named HTTP clients work as described. Caveat: cart/wishlist proxying uses a hardcoded `localhost:5009` URL instead of the named-client pattern used elsewhere, and GatewayBff has no integration at all with UserService (no auth is enforced on any of its routes).

### ⚠️ Partially real — core plumbing works, several claimed features are dead or broken

5. **ProductService** (port 5198) — CQRS/MediatR/Redis cache-aside genuinely work for basic CRUD and catalog listing. But: category search filter is silently ignored, ~9 "e-commerce" endpoints (featured/bestsellers/categories/brands/reviews) are hardcoded placeholders not backed by real data, and `InMemoryProductCache` is dead code.

6. **UserService** (port 5010) — Identity/JWT/OAuth2 registration, login, and token issuance genuinely work. But email delivery silently fails end-to-end (misconfigured/nonexistent endpoint, swallowed exceptions), the password-reset link targets a route no frontend implements, and payment-method encryption reuses a static IV.

7. **NotificationService** (port 5246) — Core send-notification/template-render/Hangfire/SignalR plumbing works, and mock-vs-real channel swapping by environment is real. But push token registration throws outside Development (targets a nonexistent table), retry/rate-limit/cache settings are all dead config, `/health` always reports healthy, and the Hangfire dashboard has no real authorization in any environment.

### ❌ Mostly facade — infrastructure exists, the advertised core feature does not work

8. **ChatbotService** (port 5055) — PostgreSQL/Redis/JWT scaffolding is real, and the embeddable JS widget is a genuine, substantial artifact. But the actual chat API (`/api/chat/*`) is dead code with no backing implementation, the "ONNX DialoGPT" NLP engine is entirely simulated (placeholder model files, no `InferenceSession`, keyword-matching only), auth uses hardcoded demo credentials, and fine-tuning is simulated client-side. Don't describe this service as "AI-powered" without this caveat.

### 📄 Stub Only / Not Implemented

- **PaymentService** (port 5034) — No business logic at all: `Program.cs` is still the default template, no Controllers/Models/Services/Data folders exist. Kafka settings are scaffolded in `appsettings` but unused.

### Orchestration

- **AppHost** (new) — .NET Aspire orchestrator (targets `net10.0`) that starts ProductService, InventoryService, OrderService, and GatewayBff together; infrastructure (Postgres/Redis/Kafka) is still managed via Docker Compose. Doesn't cover Payment/Notification/User/Customer/Chatbot.

### Frontend

- **Angular 20.1 SPA** (`distributed-order-app`, port 4200) — GatewayBff's primary, functional client.
- **CustomerWebsite** (Razor MVC, port 5100) — an independent, parallel storefront, but far less functional than its own documentation claimed: most Views don't exist (`Product/Details`, `Cart/Index`, `Checkout/Index`, `Search`, `Categories` all 500 at runtime — only the homepage is genuinely navigable), Stripe/Serilog/AutoMapper/FluentValidation/SignalR/ImageSharp packages are referenced but entirely unused, and wishlist/checkout generate a throwaway user id on every request so nothing is retrievable across visits.

---

## 📊 Project Statistics (updated)

- **Backend microservices**: 9 (OrderService, ProductService, InventoryService, PaymentService*, NotificationService, UserService, CustomerService, ChatbotService†, GatewayBff) — *PaymentService is a stub; †ChatbotService's chat feature is dead/simulated despite real supporting infrastructure
- **Orchestrators**: Docker Compose (primary) + AppHost/.NET Aspire (new, partial — 4 services)
- **Frontends**: 2 independent — Angular 20.1 SPA (functional) + CustomerWebsite (Razor MVC, mostly non-navigable — see above)
- **Shared libraries**: 1 shared project (`Shared.Messages.OrderCreatedEvent`)
- **Database**: PostgreSQL 16, one shared instance with one database per service
- **Cache**: Redis shared across Product/Inventory/Customer/Chatbot/Notification
- **Kafka topics**: 1 active topic (`order-created`); PaymentService has settings scaffolded but no consumer
- **Projects in the solution**: 14 (`DistributedOrderSystem.sln`)

### Architectural Patterns Actually Implemented

1. ✅ Microservices Architecture
2. ✅ Backend for Frontend (BFF) — with the known exception of the cart/wishlist path (hardcoded URL) and zero auth enforcement
3. ✅ Event-Driven Architecture (Kafka) — Order→Inventory works; note insufficient-stock doesn't retry, only technical failures do
4. ✅ CQRS via MediatR — ProductService (full, with real gaps in a few query handlers) and GatewayBff (partial)
5. ✅ Domain-Driven Design — **OrderService only**
6. ✅ Repository Pattern — OrderService, ProductService
7. ✅ Cache-Aside Pattern (Redis) — Product/Inventory/Chatbot, with hardcoded TTLs that ignore config in more than one service
8. ✅ Producer-Consumer Pattern (Kafka) — Order → Inventory
9. ✅ Dependency Injection

---

## 🚀 How to Use This Documentation

### For Developers
1. **Start with**: `../COMPREHENSIVE_SOLUTION_ARCHITECTURE_EN.md` (repo root).
2. **Go deeper per service**: every per-service doc listed above has now been verified against the real code — read the "Status vs. current code" column first to know which caveats to expect before trusting a code sample.
3. **Understand messaging**: `KAFKA_INTEGRATION.md`, `KAFKA_TESTING_GUIDE.md`.
4. **To finish PaymentService**: `PAYMENT_AND_NOTIFICATION_SERVICES_DESIGN.md` as a design starting point — nothing in it is built yet.
5. **To finish ChatbotService's actual chat feature**: none of the existing docs describe a real implementation to extend — `AdvancedNLPService`/`IChatbotService` need to be built from scratch, not "fixed."
6. **For CustomerWebsite**: read `SHOPVERSE_PLATFORM_DOCUMENTATION.md`'s and `VIEWCOMPONENT_FLOW_GUIDE.md`'s corrections before assuming any page beyond the homepage renders.

### For Testing
1. Follow `KAFKA_TESTING_GUIDE.md` for setup.
2. Use the curl commands in the various per-service documents — all product/order ids in examples are now `Guid`s, not integers.

### For Deployment
No CI/CD pipeline or Kubernetes manifests exist in the repository (`.github/workflows/` is empty).

---

## 🔧 Troubleshooting

- **If Kafka doesn't work** → `KAFKA_TESTING_GUIDE.md`. Containerized services use `kafka:9092`; host-launched services (including the AppHost) use `localhost:29092`.
- **If the cache doesn't work** → Cache sections in `PRODUCT_SERVICE_DOCUMENTATION.md` and `INVENTORY_SERVICE_DOCUMENTATION.md` — check whether you're hitting a hardcoded TTL that ignores `appsettings` before assuming a config change will take effect.
- **If a CustomerWebsite page 500s** → check `SHOPVERSE_PLATFORM_DOCUMENTATION.md` first; the view you're hitting may simply not exist yet.
- **If ChatbotService "doesn't respond intelligently"** → it isn't supposed to; see the ChatbotService entry above.
- **If services aren't communicating** → `../COMPREHENSIVE_SOLUTION_ARCHITECTURE_EN.md`, "Current Gaps" section.

---

## ✨ Current State in Summary

- **4 services solid** (Order, Inventory, Customer, GatewayBff), **3 partially real with broken features** (Product, User, Notification), **1 mostly facade** (Chatbot — real infra, dead/simulated core feature), **1 pure stub** (Payment).
- **2 independent frontends**: Angular SPA is functional; CustomerWebsite is mostly non-navigable (missing Views) despite its own documentation previously claiming otherwise.
- **2 local orchestration modes**: Docker Compose (complete) and AppHost/.NET Aspire (partial, 4 services).
- **No tests, no CI/CD, no Kubernetes.**
- This pass went deeper than porting ports/DB names — several documents were describing functionality (working chat AI, working Stripe checkout, working push notifications, working password reset) that doesn't actually work end-to-end. Treat every "✅ Rewritten" row above as verified against source, and everything else in this folder as unverified until it gets the same treatment.
