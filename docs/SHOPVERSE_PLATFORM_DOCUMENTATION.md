# ShopVerse Platform - Complete Documentation

> Updated to match the real code. Previous versions presented many features (Stripe, SignalR, AutoMapper, FluentValidation, Serilog, health checks, CSP headers) as implemented: in the current code these packages are referenced in the `.csproj` but **have no usage at all** in the project — no calls, no registration in `Program.cs`. Missing pages were also found (`Views/Product/Details`, `Views/Cart`, `Views/Checkout`, etc.) that make a good portion of the flows described below unreachable without a runtime error. See the "⚠️" notes for details.

## 🚀 Project Overview

**ShopVerse** (the internal project name for `CustomerWebsite`) is a second frontend, **independent** from the main Angular SPA (`distributed-order-app`, port 4200) — it doesn't replace it, it runs alongside it. It represents an alternative customer interface built with ASP.NET Core MVC/Razor, intended to demonstrate a direct integration (not through GatewayBff, except for the cart/wishlist exception — see [Microservice Integration](#-microservice-integration)) with the system's backends.

### 📊 General Information

- **Project Name**: ShopVerse CustomerWebsite
- **Framework**: ASP.NET Core 9.0 MVC (`net9.0`, confirmed in `CustomerWebsite.csproj`)
- **Real port**: **5100** (HTTP) — not 7001 as stated in some previous versions of this and other documents
- **Database**: **none**. The `Microsoft.EntityFrameworkCore.SqlServer` package is referenced in the `.csproj`, but `Program.cs` never calls `AddDbContext` — there is no `DbContext` in this project at all. Every piece of persistent data (catalog, cart, orders) lives in the backend microservices.

---

## 🎯 Architectural Goals

The goals listed below remain the project's stated goals; not all of them have been achieved in the current implementation (see the following sections for the real status).

### Primary Goals
1. Modern Customer Interface
2. Microservice Integration
3. Optimized Performance
4. Horizontal Scalability
5. Maintainability

### Secondary Goals
- SEO Friendly, Accessibility, Mobile-First, Internationalization (none of these has a verifiable implementation in the code today — they're design goals, not completed features)

---

## 🏗️ Technical Architecture

### Technology Stack — what's actually used

| Component | In `.csproj`? | Used in code? |
|------------|:---:|:---:|
| ASP.NET Core MVC 9.0 | ✅ | ✅ `AddControllersWithViews()` |
| Bootstrap 5.3 (CSS/JS) | — (via `wwwroot/lib`) | ✅ used in the views |
| HttpClient | ✅ `Microsoft.Extensions.Http` | ✅ generic `AddHttpClient()`, used in the `Services/*.cs` services |
| ASP.NET Session | ✅ | ✅ `AddDistributedMemoryCache()` + `AddSession()` — see note below |
| Logging (`Microsoft.Extensions.Logging`) | ✅ | ✅ `ClearProviders()` + `AddConsole()` + `AddDebug()` |
| **Stripe.net** | ✅ | ❌ **no reference to `Stripe.*` in any `.cs` file** — no real payment integration |
| **Serilog.AspNetCore** | ✅ | ❌ never called — the real logging is the basic one above |
| **AutoMapper** | ✅ | ❌ no DI registration, no `IMapper` injected anywhere |
| **FluentValidation.AspNetCore** | ✅ | ❌ no validators, no registration |
| **Microsoft.AspNetCore.SignalR.Client** | ✅ | ❌ no `HubConnection` anywhere in the project (neither C# nor JS) |
| **SixLabors.ImageSharp.Web** | ✅ | ❌ no usage, no middleware registered |
| **Microsoft.Extensions.Caching.StackExchangeRedis** | ✅ | ❌ the session uses `AddDistributedMemoryCache` (in-memory), not Redis |
| **Microsoft.EntityFrameworkCore.SqlServer** | ✅ | ❌ no `DbContext` — no database |
| RestSharp | ✅ | not verified in the files read — likely a leftover, not used by the main services (`ProductService.cs`/`OrderService.cs` use `HttpClient` directly) |

In short: **roughly half the packages in the `.csproj` are dead references**. This is useful to know before "adding" a feature that looks like it's already there (e.g. Stripe payments, real-time notifications via SignalR) — there's nothing to build on, it has to be written from scratch.

### Implemented Architectural Patterns

#### 1. Model-View-Controller (MVC)
Real: Controllers (`HomeController`, `ProductController`, `CartController`, `CheckoutController`) contain working logic; Models are simple DTOs/ViewModels; but — see below — **many Views don't exist yet**.

#### 2. Dependency Injection
Real: `IProductService`, `IShoppingCartService`, `IWishlistService`, `IOrderService` are registered as `Scoped` in `Program.cs` and injected into the controllers. `IWishlistService` is implemented by a `WishlistService` class defined **inside** `Services/ShoppingCartService.cs` (not in a separate file).

#### 3. "Repository Pattern (via Services)"
The services (`ProductService.cs`, `OrderService.cs`, `ShoppingCartService.cs`) wrap HTTP calls to the backends — it's not a repository in the classic sense (no access to local data), but the conceptual description of "API-call abstraction" remains accurate.

---

## 📁 Project Structure — real, with gaps on the Views

```
CustomerWebsite/
├── Controllers/
│   ├── HomeController.cs        # Index, Search, Categories, Category, About, Contact,
│   │                             # CustomerService, Autocomplete, HandleError
│   ├── CartController.cs        # Index, AddItem, UpdateQuantity, RemoveItem, Clear,
│   │                             # GetCartCount, GetCartSummary, Checkout, SaveForLater
│   ├── ProductController.cs     # Details, AddToCart, AddToWishlist, LoadReviews,
│   │                             # AddReview, Compare, QuickSearch
│   └── CheckoutController.cs    # Index, ShippingAddress, ShippingMethod, PaymentMethod,
│                                 # PlaceOrder, OrderConfirmation, ApplyPromoCode,
│                                 # RemovePromoCode, CalculateShipping
│
├── Components/
│   └── ProductCardViewComponent.cs   # synchronous, no DI — see VIEWCOMPONENT_FLOW_GUIDE.md
│
├── Models/
│   ├── ProductModels.cs, ShoppingModels.cs, OrderModels.cs, AccountModels.cs, ErrorViewModel.cs
│
├── Services/
│   ├── ProductService.cs         # calls ProductService via a direct HttpClient
│   ├── ShoppingCartService.cs    # contains BOTH ShoppingCartService AND WishlistService;
│   │                             # both call GatewayBff (not CustomerService directly)
│   └── OrderService.cs           # calls OrderService via a direct HttpClient — see notes below
│
├── Views/
│   ├── _ViewImports.cshtml, _ViewStart.cshtml
│   ├── Home/
│   │   ├── Index.cshtml          # ✅ exists, the only "product" page that's genuinely navigable
│   │   └── Privacy.cshtml        # ✅ exists
│   └── Shared/
│       ├── _Layout.cshtml, _ValidationScriptsPartial.cshtml, Error.cshtml
│       └── Components/ProductCard/Default.cshtml
│
├── wwwroot/
│   ├── css/shopverse.css, site.css
│   └── js/shopverse.js, site.js
│
├── Program.cs
└── appsettings.json
```

⚠️ **The following do not exist** (verified with an exhaustive search of the `Views/` folder): `Views/Product/Details.cshtml`, `Views/Product/Compare.cshtml`, `Views/Cart/Index.cshtml`, `Views/Checkout/Index.cshtml`, `Views/Home/Search.cshtml`, `Views/Home/Categories.cshtml`, `Views/Home/Category.cshtml`, `Views/Home/About.cshtml`, `Views/Home/Contact.cshtml`, `Views/Home/CustomerService.cshtml`, and the `Views/Product/_ProductReviews.cshtml` partial. The corresponding C# actions exist and in many cases contain complete logic (e.g. `CheckoutController` manages an entire multi-step flow), but **invoking them produces a runtime error** ("The view '...' was not found"), not a page. Only the homepage (`/` or `/Home/Index`) is genuinely navigable end-to-end in the browser today.

---

## 🎨 Design System - ShopVerse Branding

This section describes CSS styling choices (`wwwroot/css/shopverse.css`) and requires no technical corrections — the color palette, typography, and example CSS classes remain valid as a style reference.

| Color | Hex Code | Usage |
|--------|----------|----------|
| Primary Purple | `#7C3AED` | Main elements, CTAs |
| Secondary Indigo | `#4F46E5` | Links, accents |
| Tertiary Teal | `#14B8A6` | Success, confirmations |
| Neutral Gray | `#6B7280` | Secondary text |
| Pure White | `#FFFFFF` | Backgrounds, contrast |

---

## 🔧 Features — what's actually reachable

### Genuinely working
- **Homepage** (`GET /` → `HomeController.Index`): featured products (real if the backend responds, otherwise a **hardcoded list of 8 Apple/electronics products** with a `Guid.NewGuid()` generated on every render — not real products), categories, search (a form that posts to `/Home/Search`, but **`Views/Home/Search.cshtml` doesn't exist**, so the submit would fail at runtime), cart count via `ViewBag.CartItemCount`.
- **Search autocomplete** (`GET /Home/Autocomplete?term=...`): returns JSON, requires no view — works as long as the `ProductService` backend responds to `SearchProductsAsync`.
- **Add to cart via AJAX** (`POST /Cart/AddItem`, `POST /Product/AddToCart`): return JSON, architecturally functional — depend on GatewayBff/CustomerService being reachable with the correct ports (see next section).

### Present in code but unreachable from the browser (missing view)
- Product detail, product comparison, cart page, the entire checkout flow (5 steps), categories/search/about/contact/customer-service, product reviews.

### Wishlist — functionally broken independent of the missing views
`ProductController.AddToWishlist` uses `Guid.NewGuid()` as a placeholder user id **on every call** (there is no authentication):
```csharp
var userId = Guid.NewGuid(); // Placeholder for the user ID
var success = await _wishlistService.AddToWishlistAsync(userId, productId, notes);
```
This means every "add to wishlist" creates a wishlist for a random user that can never be retrieved again — it's not just that the view to display it is missing, the wishlist itself can never be looked up again with the same `userId`. The same applies to `CheckoutController.PlaceOrder`, which uses `Guid? userId = null`.

### Checkout — real details
The `CheckoutController` flow (Index → ShippingAddress → ShippingMethod → PaymentMethod → PlaceOrder → OrderConfirmation) is written as a state machine using `TempData` to serialize the `CheckoutModel` between steps. **There is no Stripe integration** despite the package being referenced — `PaymentMethod` merely computes tax (`Subtotal * 0.22m`) and moves to the next step. `ApplyPromoCode`/`CalculateShipping` call `IOrderService.ValidatePromoCodeAsync`/`GetShippingOptionsAsync`, which in turn call endpoints (`/api/promocodes/...`, `/api/shipping/options`) **that don't exist on the real `OrderService`** — see `docs/BFF_ARCHITECTURE.md` for confirmation of this point, already verified there.

---

## 🌐 Microservice Integration

CustomerWebsite uses **two different integration styles** (already documented in detail in `docs/BFF_ARCHITECTURE.md`, which is the reference source — here just a summary):

- **Cart and Wishlist** genuinely go through **GatewayBff** (`ShoppingCartService`/`WishlistService` read `Services:GatewayBff:BaseUrl`), which in turn forwards to CustomerService via a hardcoded URL.
- **Product and Order** call ProductService/OrderService **directly** (`Services:ProductService:BaseUrl`, `Services:OrderService:BaseUrl`), without going through GatewayBff.

### Ports configured in `appsettings.json` vs. real ports — ⚠️ all mismatched

| Config key | Configured URL | Service's real port |
|---|---|---|
| `GatewayBff` / `CartService` / `WishlistService` | `:7000` | **5189** (HTTP) / 7119 (HTTPS) |
| `ProductService` | `:5003` | **5198** |
| `OrderService` | `:5001` | **5003** |
| `InventoryService` | `:5002` | **5051** |
| `PaymentService` | `:5004` | **5034** |
| `NotificationService` | `:5005` | **5246** |
| `ChatbotService` | `:5006` | **5055** |

None of these configurations point at the right service today. For a full breakdown (including why each value is wrong and how to fix it) see `docs/BFF_ARCHITECTURE.md`, "Configuration" section.

---

## 📱 Responsive Design Strategy

No correction needed — these are CSS choices verifiable in `wwwroot/css/shopverse.css` and don't require validation against application logic.

---

## ⚡ Performance Optimization

The "Asset Optimization", "Caching Strategy" (`OnPrepareResponse` for `Cache-Control`), and "Lazy Loading" (Intersection Observer) sections from previous versions **have no counterpart in `Program.cs` or the real JS files** read for this update — treat them as proposals, not as already-applied optimizations. `app.UseResponseCompression()`, on the other hand, is genuinely present in `Program.cs`.

---

## 🔒 Security & Privacy

Verified against `Program.cs`:
- ✅ **HTTPS enforcement**: `app.UseHttpsRedirection()` and `app.UseHsts()` are genuinely present (HSTS only when not in Development).
- ❌ **Cookie policy with `CookiePolicyOptions` (explicit SameSite/HttpOnly/Secure)**: not present in `Program.cs` — only the session cookie configuration (`options.Cookie.HttpOnly = true`, `IsEssential = true`) exists, not a global `CookiePolicyOptions`.
- ❌ **Content Security Policy**: no middleware sets a CSP header.
- ⚠️ **Input Validation** via Data Annotations: plausible that the Models in `Models/*.cs` have attributes like `[Required]`/`[StringLength]` (a common ASP.NET Core pattern), but **FluentValidation is not wired up** despite the package being referenced.

---

## 📊 Monitoring & Logging

- ✅ Real logging: `builder.Logging.ClearProviders().AddConsole().AddDebug()` — not Serilog despite the package being referenced.
- ❌ **Health Checks**: no `AddHealthChecks()` in `Program.cs` — the example with `ProductServiceHealthCheck`/`OrderServiceHealthCheck` is aspirational, not implemented.
- The performance metrics stated in previous versions (response time, Lighthouse score) aren't measured by any tool present in the repository.

---

## 🚀 Deployment & DevOps

The Docker/production `appsettings` sections remain valid design proposals, but **no `Dockerfile` exists for `CustomerWebsite`** in the repository (unlike the other backend services, which each have one referenced from `docker/docker-compose.yml` — CustomerWebsite doesn't appear in that file).

```bash
# Real commands for running locally
cd "src/frontend/customer-facing e-commerce/CustomerWebsite"
dotnet restore
dotnet run   # http://localhost:5100 — see the notes above on the ports to fix in appsettings.json
```

---

## 🧪 Testing Strategy

**No test project exists for `CustomerWebsite`** (nor for any other project in the solution — consistent with what CLAUDE.md states). The unit/integration/E2E test examples in previous versions of this document illustrate how the project *could* be tested, not code present in the repository.

---

## 📈 Roadmap & Future Enhancements

The roadmap items (user authentication, persistent wishlist, reviews, PWA, GraphQL, micro-frontend, recommendation engine, i18n, CDN, etc.) remain valid future directions. It should be clarified, though, that some of them (user authentication, a working wishlist) are **prerequisites** for making features already present in the code consistent (see the note on `Guid.NewGuid()` as a user placeholder above), not just additional "nice to haves."

---

## 🤝 Contributing Guidelines / Support & Contacts / License

Informational, non-technical sections, unchanged from previous versions — they require no verification against the code.
