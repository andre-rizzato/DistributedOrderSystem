# Backend for Frontend (BFF) Architecture

## Overview

`CustomerWebsite` (the Razor MVC storefront under `src/frontend/customer-facing e-commerce/`) uses **two different integration styles at once**, depending on the feature:

- **Cart and Wishlist** genuinely go through GatewayBff, as this document originally described. `ShoppingCartService`/`WishlistService` in CustomerWebsite call GatewayBff's `CartBffController`/`WishlistBffController`, which in turn call CustomerService via MediatR commands.
- **Products and Orders bypass GatewayBff entirely.** CustomerWebsite's `ProductService`/`OrderService` (the local `Services/` classes, not the backend microservices of the same name) call ProductService and OrderService **directly**.

Neither path is currently reachable as configured, though: the ports baked into `CustomerWebsite/appsettings.json` predate the services' current port assignments (see [Configuration](#configuration) below), and several endpoints CustomerWebsite's `OrderService.cs` calls (`/api/orders/history`, `/api/shipping/options`, `/api/promocodes/...`) don't exist on the real `OrderService` at all. Treat this document as "how the code is wired," not "what works out of the box."

## Architecture Flow

### Cart & Wishlist (actually routed through the BFF)

```
CustomerWebsite (Services/ShoppingCartService.cs, WishlistService.cs)
    ↓ HTTP, config key "Services:GatewayBff:BaseUrl"
GatewayBff — CartBffController (/api/cart) / WishlistBffController (/api/wishlist)
    ↓ MediatR command/query handlers, each using an ad-hoc HttpClient with the
    ↓ CustomerService URL hardcoded to http://localhost:5009 (not the named/
    ↓ configured HttpClient pattern used for Product/Inventory/Order)
CustomerService — CartController / WishlistController
    ↓
Redis (only store — CustomerService has no database)
```

### Products & Orders (direct, no BFF)

```
CustomerWebsite (Services/ProductService.cs, OrderService.cs)
    ↓ HTTP, config keys "Services:ProductService:BaseUrl" / "Services:OrderService:BaseUrl"
    ↓ (GatewayBff is not involved on this path)
ProductService  /  OrderService
```

## Endpoint Mapping

The route *shapes* match one-to-one between GatewayBff and CustomerService (GatewayBff is a thin proxy here, not an aggregator) — only the base URL differs.

### Cart Operations

| CustomerWebsite Call | GatewayBff Endpoint | CustomerService Endpoint |
|---------------------|---------------------|--------------------------|
| GET Cart | `GET /api/cart/{sessionId}` | `GET /api/cart/{sessionId}` |
| Add Item | `POST /api/cart/{sessionId}/items` | `POST /api/cart/{sessionId}/items` |
| Update Item | `PUT /api/cart/{sessionId}/items/{cartItemId}` | `PUT /api/cart/{sessionId}/items/{cartItemId}` |
| Remove Item | `DELETE /api/cart/{sessionId}/items/{cartItemId}` | `DELETE /api/cart/{sessionId}/items/{cartItemId}` |
| Clear Cart | `DELETE /api/cart/{sessionId}` | `DELETE /api/cart/{sessionId}` |

### Wishlist Operations

| CustomerWebsite Call | GatewayBff Endpoint | CustomerService Endpoint |
|---------------------|---------------------|--------------------------|
| GET Wishlist | `GET /api/wishlist/{userId}` | `GET /api/wishlist/{userId}` |
| Add Item | `POST /api/wishlist/{userId}/items` | `POST /api/wishlist/{userId}/items` |
| Remove Item | `DELETE /api/wishlist/{userId}/items/{productId}` | `DELETE /api/wishlist/{userId}/items/{productId}` |
| Move to Cart | `POST /api/wishlist/{userId}/items/{productId}/move-to-cart` | `POST /api/wishlist/{userId}/items/{productId}/move-to-cart` |

## GatewayBff Components

### DTOs (Contracts)
- **CartDtos.cs**: `CartDto`, `CartItemDto`, `AddToCartRequest`, `UpdateCartItemRequest`
- **WishlistDtos.cs**: `WishlistDto`, `WishlistItemDto`, `AddToWishlistRequest`, `MoveToCartRequest`

### Queries (read side)
- `GetCartQuery` / `GetCartQueryHandler`
- `GetWishlistQuery` / `GetWishlistQueryHandler`

Both call `_clients.CreateClient()` (the default, unnamed `IHttpClientFactory` client) against `http://localhost:5009/...` directly in the handler.

### Commands (write side)
- `AddToCartCommand`, `UpdateCartItemCommand`, `RemoveFromCartCommand`, `ClearCartCommand`
- `AddToWishlistCommand`, `RemoveFromWishlistCommand`, `MoveWishlistToCartCommand`

All eight cart/wishlist handlers (2 queries + 6 commands, `MoveWishlistToCartCommand` included) follow the same pattern: an unnamed `HttpClient` with `http://localhost:5009` hardcoded inline. This is consistent across the whole cart/wishlist vertical — it isn't one handler that was overlooked, it's the pattern used throughout that slice. It differs from `CommandsController`/`QueriesController` (catalog, orders, inventory), which use named clients (`"ProductService"`, `"InventoryService"`, `"OrderService"`) configured from `ServiceUrls` in `appsettings`.

### Controllers
- `CartBffController.cs` — `/api/cart`
- `WishlistBffController.cs` — `/api/wishlist`

## Configuration

### Actual current values

**GatewayBff** (`appsettings.Development.json`) — no CustomerService entry exists here, because the cart/wishlist handlers don't read `ServiceUrls`; the address is hardcoded in each handler instead:
```json
"ServiceUrls": {
  "ProductService": "http://localhost:5198",
  "InventoryService": "http://localhost:5051",
  "OrderService": "http://localhost:5003"
}
```

**CustomerWebsite** (`appsettings.json`) — every one of these URLs is stale relative to the services' current ports:
```json
"Services": {
  "GatewayBff":        { "BaseUrl": "https://localhost:7000" },
  "ProductService":     { "BaseUrl": "https://localhost:5003" },
  "OrderService":       { "BaseUrl": "https://localhost:5001" },
  "InventoryService":   { "BaseUrl": "https://localhost:5002" },
  "PaymentService":     { "BaseUrl": "https://localhost:5004" },
  "NotificationService": { "BaseUrl": "https://localhost:5005" },
  "ChatbotService":     { "BaseUrl": "https://localhost:5006" },
  "CartService":        { "BaseUrl": "https://localhost:7000" },
  "WishlistService":     { "BaseUrl": "https://localhost:7000" }
}
```

| Config key | Configured URL | Actual current port | Correct? |
|---|---|---|---|
| `GatewayBff` / `CartService` / `WishlistService` | `:7000` | `5189` (HTTP) / `7119` (HTTPS) | ❌ stale |
| `ProductService` | `:5003` | `5198` | ❌ stale (`:5003` is actually OrderService's port) |
| `OrderService` | `:5001` | `5003` | ❌ stale |
| `InventoryService` | `:5002` | `5051` | ❌ stale |
| `PaymentService` | `:5004` | `5034` | ❌ stale |
| `NotificationService` | `:5005` | `5246` | ❌ stale |
| `ChatbotService` | `:5006` | `5055` | ❌ stale |

To actually run CustomerWebsite end-to-end against the current backend, update `Services:GatewayBff:BaseUrl` (and `CartService`/`WishlistService`, which read the same key) to `http://localhost:5189`, and the rest to their real ports from the table above — and either drop HTTPS or configure it to match, since every backend service here runs plain HTTP in Development (`ASPNETCORE_URLS=http://+:80` in Docker, `http://localhost:<port>` in the local launch profile).

### Service Implementation

```csharp
// ShoppingCartService.cs / WishlistService.cs — both read the same config key
_cartServiceBaseUrl = _configuration.GetValue<string>("Services:GatewayBff:BaseUrl")
                     ?? "https://localhost:7000";
```

```csharp
// GatewayBff/Commands/AddToCartCommand.cs — the address is not read from config at all
var response = await client.PostAsJsonAsync(
    $"http://localhost:5009/api/cart/{request.SessionId}/items", payload, cancellationToken);
```

## What This Design Actually Gets You

- **Cart/Wishlist**: CustomerWebsite only needs to know about GatewayBff's URL for these two features — CustomerService (and the fact that it's Redis-backed) is hidden from the frontend.
- **Products/Orders**: no such indirection — CustomerWebsite talks to those services' real ports/routes, so any change to ProductService's or OrderService's contract is a breaking change for CustomerWebsite directly.
- There is **no aggregation** happening on the cart/wishlist path today — GatewayBff proxies one-to-one, it doesn't combine cart data with product details. (`ShoppingCartService.GetCartAsync` does its own client-side enrichment afterward, calling `IProductService.GetProductByIdAsync` per line item.)
- There is **no authentication, rate limiting, or response caching** at the GatewayBff layer currently — none of the "Benefits" historically claimed for this setup (security boundary, centralized auth, request throttling) are implemented in code today.

## Known Gaps

1. **Stale ports in `CustomerWebsite/appsettings.json`** — see table above. This is the main reason the cart/wishlist and product/order flows won't work without manual correction.
2. **Hardcoded `localhost:5009` in eight GatewayBff handlers** — works for local development against the default CustomerService port, but doesn't respect `ServiceUrls`/environment configuration the way the Product/Inventory/Order path does. Any change to CustomerService's port or a containerized deployment (where the hostname would be `customer-service`, not `localhost`) requires editing these handlers directly.
3. **CustomerWebsite's `OrderService.cs` calls endpoints that don't exist on the real OrderService** — `/api/orders/history`, `/api/orders/{id}/cancel`, `/api/shipping/options`, `/api/promocodes/{code}/validate` are not implemented anywhere in `OrderService`'s `OrdersController`. This part of CustomerWebsite is effectively unfinished/aspirational, independent of the port issue.
4. **Products and Orders don't go through GatewayBff at all** — if the intent (per this document's original framing) was for CustomerWebsite to route everything through the BFF, that migration was only completed for cart and wishlist.

## Testing

### Start Services (current real ports)
```powershell
# 1. Infra (Postgres, Redis, Kafka)
docker compose -f docker/docker-compose.yml up -d dos_postgres dos_redis dos_kafka

# 2. CustomerService
dotnet run --project src/CustomerService        # http://localhost:5009

# 3. GatewayBff
dotnet run --project src/GatewayBff              # http://localhost:5189

# 4. ProductService / OrderService (needed for the direct, non-BFF path)
dotnet run --project src/ProductService          # http://localhost:5198
dotnet run --project src/OrderService            # http://localhost:5003

# 5. CustomerWebsite — update appsettings.json ports first (see Configuration)
dotnet run --project "src/frontend/customer-facing e-commerce/CustomerWebsite"  # http://localhost:5100
```

### Test Flow
1. Access CustomerWebsite at `http://localhost:5100`.
2. Add an item to cart → CustomerWebsite → GatewayBff (`:5189`) → CustomerService (`:5009`) → Redis.
3. View cart → GatewayBff returns the raw cart; CustomerWebsite enriches each line with product details via its own direct call to ProductService.
4. Add to wishlist / move to cart → same GatewayBff → CustomerService path as cart.
5. Browse products or place an order → CustomerWebsite calls ProductService/OrderService directly; GatewayBff is not involved.

## Ports Reference (current, verified against `launchSettings.json` / `docker-compose.yml`)

| Service | Port (HTTP) | Purpose |
|---------|------|---------|
| CustomerWebsite | 5100 | Independent Razor MVC storefront |
| GatewayBff | 5189 (7119 HTTPS) | BFF — proxies cart/wishlist, aggregates catalog, proxies order/product/inventory commands |
| CustomerService | 5009 | Cart & wishlist (Redis-only) |
| ProductService | 5198 | Product catalog |
| OrderService | 5003 | Order management |
| InventoryService | 5051 | Inventory tracking |
| Redis | 6379 | Cache / cart & wishlist store |

For the full, current picture of every service (not just this cart/wishlist slice), see `../COMPREHENSIVE_SOLUTION_ARCHITECTURE_EN.md` at the repository root.
