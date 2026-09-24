The BFF-migration research is done (dead-code cleanup is still running — I'll report that separately when it finishes).

Effort estimate: routing all frontend calls through GatewayBff

Angular app: already done. All three services (product.ts, order.ts, inventory at localhost:5189 exclusively — zero direct calls to bypass.

CustomerWebsite (Razor): 18 direct-call sites across 2 files need migration:

┌────────────────────────────┬────────────────────────┬───────────┬──────────────────────────────────────────────────────────────────────────────────────┐
│            File            │         Target         │ Endpoints │                                          Already has a BFF equivalent?                                           │
├────────────────────────────┼────────────────────────┼───────────┼──────────────────────────────────────────────────────────────────────────────────────┤
│ Services/ProductService.cs │ ProductService         │ 11        │ 3 yes (catalog, catalog/{id}) / 8 no (search, featured, bestsellers, recommended, related, categories, brands,   │
│                            │ directly               │           │ reviews)                                                                             │
├────────────────────────────┼────────────────────────┼───────────┼──────────────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Services/OrderService.cs   │ OrderService directly  │ 7         │ 2 yes (getory, cancel, shipping-options, promo-validate, promo-discount)             │
└────────────────────────────┴────────────────────────┴───────────┴──────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘

Cart and wishlist already go through CartBffController/WishlistBffController — no work needed there.

Important side-finding: 5 of those 7 OrderService.cs calls (history, cancel, shipping options, promo validate/discount) hit endpoints that don't exist on OrderService at all — its real OrdersController only has list/get/create/update-status. Those calls are alrea of BFF routing. Also appsettings.json has stale ports (ProductService listedas :5003, actually :5198; OrderService listed as :5001, actually :5003) — worth fixing regardless.

Effort breakdown

┌─────────────────────────────────────────────────────────────────────────────────────────────────────────────┬─────────────┐
│                                                    Scope                        │    Days     │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┼─────────────┤
│ New GatewayBff proxy handlers for the 9 missing Product endpoints               │ 4–5         │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┼─────────────┤
│ Extend CreateOrderCommand (needs shipping/billing/payment/promo, not just itery │ 1.5–2       │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┼─────────────┤
│ Cancel/Shipping/Promo BFF handlers (assuming backend logic already existed)     │ 1.5–2       │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┼─────────────┤
│ CustomerWebsite: repoint 11 Product calls, reconcile response shapes            │ 1.5–2       │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┼─────────────┤
│ CustomerWebsite: repoint Order calls, replace dynamic deserialization with t    │ 1–1.5       │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┼─────────────┤
│ Manual end-to-end verification (no automated tests exist in the repo)           │ 2–3         │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┼─────────────┤
│ Subtotal (pure BFF migration)                                                   │ ~12–16 days │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┼─────────────┤
│ + if Cancel/History/Shipping/Promo need real backend implementation (they cu    │ +3–5 days   │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────────┼─────────────┤
│ Total range                                                                     │ 12–21 days  │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────────┴─────────────┘

Key assumption driving the range: whether you actually build the missing order features (cancel, history, shipping options, promo codes) for real, or just stub them the way          AgentService's cancel/create/update stubs do. Stubbing lands you at ~12 days; s to ~21. One developer familiar with the codebase's MediatR/BFF pattern; noCORS/auth rework needed since CustomerWebsite doesn't call any authenticated endpoints today; the biggest schedule risk is that all verification will be manual since there's no test suite.