# UserService - Complete Authentication & User Management

> Updated to match the actual code. The previous version stated SQL Server; the real database is PostgreSQL. Several other claims (email sending, response expiry, CORS, GatewayBff integration) turned out to be broken or aspirational rather than working — see the "⚠️" notes below.

## Overview
Comprehensive user management microservice with authentication, social login, profile management, addresses, and payment methods.

**Port:** 5010 (HTTP) / 5011 (HTTPS) — confirmed in `launchSettings.json`
**Database:** PostgreSQL (`UserServiceDb`, via `Npgsql.EntityFrameworkCore.PostgreSQL`) — not SQL Server
**Swagger/Scalar:** https://localhost:5011/scalar/v1

## Features

### ✅ Authentication
- **Email/Password Registration** - standard registration; passwords hashed by ASP.NET Core Identity (PBKDF2 under the hood, not literally "BCrypt" despite the package name pattern elsewhere in the solution — Identity's default hasher is used here, no custom BCrypt call in this service)
- **Login** - JWT-based, access token + refresh token
- **Social Login** - Google and Facebook OAuth *configured*, but ⚠️ `AuthController.SocialLogin` has a literal `// TODO: Validare il token con Google/Facebook API — Per ora, accetta qualsiasi token` — it currently trusts whatever string the client sends as the provider token, matching it against a stored `GoogleId`/`FacebookId` or creating a new user. This is real, working code, but it is not verifying the token against Google/Facebook at all.
- **Email Verification** - token-based, via `UserManager.GenerateEmailConfirmationTokenAsync`/`ConfirmEmailAsync` — the verification link itself is correctly built to point back at this service's own `GET /api/auth/verify-email` route.
- **Password Reset** - ⚠️ `ForgotPassword` builds the reset link as `{scheme}://{host}/reset-password?...` — a **frontend** route, not an API route, and no frontend in this repository implements `/reset-password`. Unlike the verification link, this one has nowhere to land as configured today.
- **Change Password** - authenticated, revokes all refresh tokens on success (real, works)
- **JWT Tokens** - access token (config-driven expiry, see below) + refresh token (7 days, hardcoded)
- **Token Revocation / Rotation** - ⚠️➡️✅ genuinely implemented: `POST /api/auth/refresh` revokes the presented refresh token and issues a new one (`TokenService.RevokeRefreshTokenAsync` + `GenerateRefreshToken`), not just modeled in the schema.
- **Lockout** - real: `SignInManager.CheckPasswordSignInAsync(..., lockoutOnFailure: true)` combined with `Program.cs`'s `Lockout.MaxFailedAccessAttempts = 5` / `DefaultLockoutTimeSpan = 15 min`.

### 👤 User Profile / 📍 Addresses / 💳 Payment Methods
Structurally as previously documented — see [API Endpoints](#api-endpoints) below, verified route-by-route against the controllers.

## Architecture

```
UserService (Port 5010 HTTP / 5011 HTTPS)
    ├── ASP.NET Core Identity (IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>)
    ├── JWT Bearer + Google/Facebook OAuth handlers (Program.cs)
    ├── Controllers
    │   ├── AuthController        (/api/auth/*)
    │   ├── UsersController       (/api/users/*)          [Authorize]
    │   ├── AddressesController   (/api/users/{userId}/addresses)   [Authorize]
    │   └── PaymentMethodsController (/api/users/{userId}/payment-methods) [Authorize]
    ├── Services
    │   ├── TokenService              (JWT + refresh token generation/rotation — real)
    │   ├── CommunicationService      (typed HttpClient — real code, but see ⚠️ Email Integration below)
    │   └── PaymentEncryptionService  (AES encryption — real, see ⚠️ Security Features below)
    └── Data: UserDbContext (EF Core + PostgreSQL)
```

## API Endpoints

Verified directly against each controller's route attributes — all match what was previously documented; no drift found here.

### 🔐 Authentication (`/api/auth`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/register` | Register new user | ❌ |
| POST | `/login` | Login with email/password | ❌ |
| POST | `/social-login` | Login with Google/Facebook (⚠️ no real token verification, see above) | ❌ |
| POST | `/refresh` | Refresh access token (real rotation) | ❌ |
| POST | `/logout` | Revoke refresh token | ✅ |
| GET | `/verify-email?token={token}&email={email}` | Verify email address | ❌ |
| POST | `/forgot-password` | Request password reset (⚠️ link points nowhere, see above) | ❌ |
| POST | `/reset-password` | Reset password with token | ❌ |
| POST | `/change-password` | Change password (authenticated) | ✅ |
| GET | `/profile/{userId}` | ⚠️ Dummy stub — exists only so `Register`'s `CreatedAtAction` has a route to point at; returns an empty `200 OK` and does nothing. Not the real profile endpoint — that's `GET /api/users/{userId}` below. Hidden from Swagger (`[ApiExplorerSettings(IgnoreApi = true)]`). | ❌ |

### 👤 Users (`/api/users`) — all require auth

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/me` | Current user profile |
| GET | `/{userId}` | User profile by ID (no ownership check — any authenticated user can look up any other user's profile by ID) |
| PUT | `/me` | Update profile |
| PUT | `/me/preferences` | Update preferences |
| DELETE | `/me` | Soft-delete (`IsActive = false`) |

### 📍 Addresses (`/api/users/{userId}/addresses`) — all require auth + ownership

Every action calls `CanAccessUser(userId)` first, which compares the route's `userId` against the JWT's `NameIdentifier` claim and `Forbid()`s on mismatch — this is real and correctly applied to every action in both `AddressesController` and `PaymentMethodsController`. (Note the asymmetry with `UsersController.GetUserById`, which has no such check.)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | Get all addresses |
| GET | `/{addressId}` | Get address by ID |
| POST | `/` | Create new address |
| PUT | `/{addressId}` | Update address |
| DELETE | `/{addressId}` | Delete address |
| POST | `/{addressId}/set-default` | Set as default (clears the flag on other addresses of the same `Type` first) |

### 💳 Payment Methods (`/api/users/{userId}/payment-methods`) — all require auth + ownership

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/` | Get all payment methods |
| GET | `/{paymentMethodId}` | Get payment method |
| POST | `/` | Add payment method (basic length check: 13-19 digits after stripping spaces/dashes) |
| DELETE | `/{paymentMethodId}` | Delete payment method |
| POST | `/{paymentMethodId}/set-default` | Set as default |

## Database Schema (PostgreSQL, real table names from `UserDbContext.OnModelCreating`)

Identity tables are explicitly renamed away from the ASP.NET Identity defaults: `Roles`, `UserRoles`, `UserClaims`, `UserLogins`, `UserTokens`, `RoleClaims`. All custom entity primary keys default to `gen_random_uuid()` (Postgres function), not SQL Server's `NEWSEQUENTIALID()`.

**Users** (`ApplicationUser : IdentityUser<Guid>`)
- Standard Identity columns (Id, Email, PasswordHash, ...) plus: FirstName, LastName, DateOfBirth, ProfilePictureUrl, PreferredLanguage (default `it-IT`), PreferredCurrency (default `EUR`), EmailNotificationsEnabled/SmsNotificationsEnabled/PushNotificationsEnabled, IsPrimeMember, PrimeMembershipExpiry, LoyaltyPoints, GoogleId, FacebookId, CreatedAt, LastLoginAt, IsActive
- Unique index on Email; non-unique indexes on GoogleId and FacebookId

**Addresses** — FullName, AddressLine1/2, City, StateProvince, PostalCode, CountryCode (default `IT`), PhoneNumber, Type (`Shipping=1`/`Billing=2`/`Both=3`), IsDefault, CreatedAt, UpdatedAt. Cascade-deletes with the user.

**PaymentMethods** — Type (`CreditCard=1`/`DebitCard=2`/`PayPal=3`/`BankTransfer=4`), CardHolderName, Last4Digits (plain text), CardBrand, ExpiryMonth/Year, EncryptedToken, IsDefault, CreatedAt, UpdatedAt. `IsExpired` is a computed property (`new DateTime(ExpiryYear, ExpiryMonth, 1) < DateTime.UtcNow`), not a stored column. Cascade-deletes with the user.

**RefreshTokens** — Token (unique), ExpiresAt (7 days from creation), CreatedAt, CreatedByIp, IsRevoked, RevokedAt, RevokedByIp, `ReplacedByToken` (⚠️ column exists but `TokenService` never sets it — the rotation chain isn't actually tracked, only revocation timestamps are). `IsActive`/`IsExpired` are computed properties.

## Configuration

### appsettings.json (real values)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=UserServiceDb;Username=postgres;Password=YourStrong_Password123;"
  },
  "Jwt": {
    "Secret": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!123",
    "Issuer": "UserService",
    "Audience": "DistributedOrderSystem",
    "ExpiresInMinutes": "15"
  },
  "Encryption": {
    "Key": "32CharacterEncryptionKey12345",
    "IV": "16CharacterIV123"
  },
  "Authentication": {
    "Google": { "ClientId": "...", "ClientSecret": "..." },
    "Facebook": { "AppId": "...", "AppSecret": "..." }
  },
  "Services": {
    "CommunicationService": { "BaseUrl": "https://localhost:5011" }
  }
}
```

⚠️ **`Services:CommunicationService:BaseUrl` defaults to `https://localhost:5011` — this service's own port.** `CommunicationService.cs` POSTs to `{BaseUrl}/api/emails/send` for every verification/welcome/reset/password-changed email. Two independent problems here:
1. The URL is self-referential (UserService calling itself), not pointing at NotificationService (the service that actually sends email/SMS/push in this solution, on port 5246).
2. Even if the URL were corrected to NotificationService's real port, **`/api/emails/send` doesn't exist there either** — `NotificationController`'s real routes are `POST /api/notification/send-template`, `/send-direct`, `/send-bulk`, `/schedule`, not `/api/emails/send`.

Because every call in `CommunicationService.cs` wraps its `HttpClient` call in try/catch and only logs on failure, **no email is ever actually sent today, and nothing surfaces this to the caller** — registration, password reset, etc. all "succeed" from the API's point of view while the email step silently fails every time.

### JWT expiry: config vs. what's actually reported
`TokenService.GenerateAccessToken` correctly reads `Jwt:ExpiresInMinutes` from config for the token's real expiry claim. But `AuthController` builds every `AuthResponse` (`Register`, `Login`, `SocialLogin`, `RefreshToken`) with `DateTime.UtcNow.AddMinutes(15)` **hardcoded inline**, not read from config. Today both values happen to agree (config is `"15"`), but changing `Jwt:ExpiresInMinutes` would desync the token's real expiry from what the API tells the client it is.

## Security Features (verified against code)

### Password Security
- ASP.NET Core Identity's default hasher (not a custom BCrypt call in this service)
- Requirements enforced via `IdentityOptions.Password.*` in `Program.cs`: digit, lowercase, uppercase required; non-alphanumeric not required; min length 8
- Lockout: 5 failed attempts → 15-minute lockout (real, see Authentication section)

### Token Security
- Access token: HMAC-SHA256, config-driven expiry (see caveat above about the response field)
- Refresh token: 64 random bytes, base64-encoded, 7-day expiry, rotated on every `/refresh` call
- ⚠️ CORS in `Program.cs` allows only `http://localhost:3000`, `https://localhost:7001`, `https://localhost:7000` — **none of these match any service actually running in this solution** (Angular SPA is `:4200`, GatewayBff is `:5189`/`:7119`, CustomerWebsite is `:5100`). As configured, a browser call from any current frontend would be blocked by CORS if it needed credentialed cross-origin access to this service.

### Payment Security
- AES encryption is real (`PaymentEncryptionService`, `System.Security.Cryptography.Aes`), CVV is never modeled/stored, only last 4 digits are stored in plain text
- ⚠️ The AES `Key` and `IV` are both static, read once from config and reused for **every** encryption call — with CBC mode (the default `Aes.Create()` mode), reusing the same IV across records means identical card numbers always encrypt to identical ciphertext. A real per-record random IV (stored alongside the ciphertext) would be the standard fix; this is a genuine cryptographic weakness in the current code, not just a "use a vault in production" caveat.

## Roles

`Admin`, `Customer`, `Vendor` are seeded at startup in `Program.cs` (`RoleManager.CreateAsync` for each, if not already present). New registrations are assigned `Customer` by default in `AuthController.Register`/`SocialLogin`.

## Integration with Other Services

### GatewayBff — ⚠️ not integrated today
GatewayBff has **no HttpClient registered for UserService** and **no JWT authentication configured** in its `Program.cs` — `CommandsController`/`QueriesController` and the cart/wishlist controllers all run without any `[Authorize]` attribute or auth middleware. The JWT-validation code sample below is illustrative of how it *could* be wired, not a description of what exists:

```csharp
// Illustrative only — GatewayBff/Program.cs has no such block today
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* ValidIssuer = "UserService", ValidAudience = "DistributedOrderSystem" */ });
```

### ChatbotService — separate, not delegating to UserService
`ChatbotService.Services.AuthenticationService` implements its own `IAuthenticationService` locally; despite the similar name, it does not call UserService for authentication. The two auth systems are independent today.

### CustomerWebsite
No code in `CustomerWebsite` currently calls UserService (no `Services:UserService:BaseUrl` config key, no controller referencing it) — the "session storage" integration sketch below is a suggested pattern, not existing code.

```csharp
// Illustrative only — not found anywhere in CustomerWebsite's current controllers
var response = await _httpClient.PostAsJsonAsync("https://localhost:5011/api/auth/login", new { model.Email, model.Password });
```

## Setup & Testing

### 1. Database
```powershell
cd src/UserService
dotnet ef migrations add InitialCreate
dotnet ef database update
```
(Confirm a Postgres instance is reachable at the connection string above — via `docker compose -f docker/docker-compose.yml up -d dos_postgres` — before running this.)

### 2. Run
```powershell
dotnet run --project src/UserService
```
- HTTP: http://localhost:5010
- HTTPS: https://localhost:5011
- Scalar: https://localhost:5011/scalar/v1

### 3. Register / Login
```powershell
curl -X POST https://localhost:5011/api/auth/register `
  -H "Content-Type: application/json" `
  -d '{"email":"test@example.com","password":"Test123!","firstName":"John","lastName":"Doe"}'
```
Expect a `201 Created` with an `AuthResponse` — but do **not** expect a verification email to actually arrive (see the Email Integration caveat above).

## Production Checklist

Still valid as a checklist of what's missing before this service is production-ready; nothing here has been implemented:
- [ ] Real Google/Facebook token verification (currently a TODO that accepts any token)
- [ ] Fix `CommunicationService`'s target URL and the missing `/api/emails/send` route so email actually sends
- [ ] Per-record random IV for payment encryption (not a static key+IV pair)
- [ ] Read `AuthResponse.ExpiresAt` from `Jwt:ExpiresInMinutes` instead of a hardcoded `15`
- [ ] Fix the CORS origin list to match real frontend ports
- [ ] Change JWT secret to a strong random key; use a vault for it and the encryption key/IV
- [ ] Rate limiting, CAPTCHA on register/login
- [ ] Wire JWT validation into GatewayBff if the intent is for it to gate the other services

## Ports Reference (corrected)

| Service | Port | Purpose |
|---------|------|---------|
| UserService | 5010 (HTTP) / 5011 (HTTPS) | Authentication & user management |
| GatewayBff | 5189 (HTTP) / 7119 (HTTPS) | API Gateway — **no JWT validation wired in today** |
| CustomerWebsite | 5100 | Independent Razor MVC storefront — **does not call UserService today** |
| NotificationService | 5246 | Where email/SMS/push actually happens — **not currently reached by UserService** despite `CommunicationService.cs` intending to call it |

---

**Status:** Authentication, profile, address, and payment-method CRUD are real and functional against a running PostgreSQL instance. Email delivery, social-login token verification, GatewayBff integration, and CORS for the current frontends are not — see the checklist above before relying on this service beyond local, unauthenticated-frontend testing.
