# Backend for Frontend (BFF) Architecture

## Overview
CustomerWebsite now routes all cart and wishlist operations through the GatewayBff (port 7000), implementing the Backend for Frontend pattern. This provides a unified API layer, request aggregation, and better separation of concerns.

## Architecture Flow

```
CustomerWebsite (Frontend)
    ↓
    ↓ HTTPS requests
    ↓
GatewayBff (Port 7000)
    ↓
    ↓ HTTP requests (internal)
    ↓
CustomerService (Port 5009)
    ↓
    ↓ Redis
    ↓
Redis Database (Port 6379)
```

## Endpoints Mapping

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
- **CartDtos.cs**: Cart data transfer objects
  - `CartDto`: Complete cart with items
  - `CartItemDto`: Individual cart item
  - `AddToCartRequest`: Add item payload
  - `UpdateCartItemRequest`: Update quantity payload

- **WishlistDtos.cs**: Wishlist data transfer objects
  - `WishlistDto`: Complete wishlist with items
  - `WishlistItemDto`: Individual wishlist item
  - `AddToWishlistRequest`: Add item payload
  - `MoveToCartRequest`: Move to cart payload

### Queries (CQRS Read Operations)
- **GetCartQuery.cs**: Retrieve cart for a session
- **GetWishlistQuery.cs**: Retrieve wishlist for a user

### Commands (CQRS Write Operations)
- **AddToCartCommand.cs**: Add item to cart
- **UpdateCartItemCommand.cs**: Update cart item quantity
- **RemoveFromCartCommand.cs**: Remove item from cart
- **ClearCartCommand.cs**: Clear entire cart
- **AddToWishlistCommand.cs**: Add item to wishlist
- **RemoveFromWishlistCommand.cs**: Remove item from wishlist
- **MoveWishlistToCartCommand.cs**: Move wishlist item to cart

### Controllers
- **CartBffController.cs**: REST API for cart operations
- **WishlistBffController.cs**: REST API for wishlist operations

## Configuration

### CustomerWebsite (appsettings.json)
```json
"Services": {
  "GatewayBff": {
    "BaseUrl": "https://localhost:7000"
  },
  "CartService": {
    "BaseUrl": "https://localhost:7000"
  },
  "WishlistService": {
    "BaseUrl": "https://localhost:7000"
  }
}
```

### Service Implementation
```csharp
// ShoppingCartService.cs
_cartServiceBaseUrl = _configuration.GetValue<string>("Services:GatewayBff:BaseUrl") ?? 
                     "https://localhost:7000";

// WishlistService.cs  
_wishlistServiceBaseUrl = _configuration.GetValue<string>("Services:GatewayBff:BaseUrl") ?? 
                         "https://localhost:7000";
```

## Benefits

### 1. **Separation of Concerns**
- Frontend only knows about BFF
- Backend services can change without affecting frontend
- BFF handles API aggregation and transformation

### 2. **Security**
- CustomerService not exposed directly to internet
- BFF can add authentication/authorization layer
- Rate limiting and request validation at BFF level

### 3. **Performance**
- BFF can aggregate multiple service calls
- Response caching at BFF level
- Reduce number of frontend HTTP calls

### 4. **Flexibility**
- Easy to add new backend services
- Frontend changes independent of backend
- BFF can provide tailored responses for different clients (web, mobile)

### 5. **Monitoring**
- Centralized logging at BFF
- Request tracking across services
- Performance metrics collection

## Future Enhancements

### 1. **Aggregation Queries**
Implement enriched endpoints that combine data from multiple services:

```csharp
// GET /api/cart/{sessionId}/enriched
// Returns: Cart + Product details + Inventory status + Pricing
public class GetEnrichedCartQuery : IRequest<EnrichedCartDto>
{
    // Calls CustomerService, ProductService, InventoryService
    // Returns unified response with all data
}
```

### 2. **Product Service Integration**
Route product calls through BFF:
```csharp
// CustomerWebsite -> GatewayBff -> ProductService
"Services": {
  "ProductService": {
    "BaseUrl": "https://localhost:7000"  // Use BFF instead of direct
  }
}
```

### 3. **Caching Layer**
Add Redis caching at BFF level:
- Cache product details
- Cache cart/wishlist data with short TTL
- Invalidate on updates

### 4. **Authentication/Authorization**
Add JWT validation at BFF:
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { ... });
```

### 5. **API Gateway Features**
- Rate limiting per client
- Request/response transformation
- API versioning
- Circuit breakers for resilience

## Testing

### Start Services
```powershell
# 1. Start Redis
docker run -d -p 6379:6379 redis

# 2. Start CustomerService
cd src/CustomerService
dotnet run

# 3. Start GatewayBff  
cd src/GatewayBff
dotnet run

# 4. Start CustomerWebsite
cd "src/frontend/customer-facing e-commerce/CustomerWebsite"
dotnet run
```

### Test Flow
1. Access CustomerWebsite at `https://localhost:7001`
2. Add items to cart → Routed through BFF → Stored in CustomerService → Redis
3. View cart → BFF aggregates cart + product data
4. Add to wishlist → Routed through BFF → Stored in CustomerService → Redis
5. Move wishlist to cart → BFF orchestrates the move operation

## Documentation

- **GatewayBff Swagger**: https://localhost:7000/scalar/v1
- **CustomerService Swagger**: http://localhost:5009/scalar/v1
- **CustomerWebsite**: https://localhost:7001

## Ports Reference

| Service | Port | Purpose |
|---------|------|---------|
| CustomerWebsite | 7001 | Frontend web application |
| GatewayBff | 7000 | Backend for Frontend (API aggregation) |
| CustomerService | 5009 | Cart & Wishlist microservice |
| ProductService | 5003 | Product catalog service |
| OrderService | 5001 | Order management service |
| InventoryService | 5002 | Inventory tracking service |
| Redis | 6379 | Cache and data store |
