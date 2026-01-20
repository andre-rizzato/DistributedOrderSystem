# UserService - Complete Authentication & User Management

## Overview
Comprehensive user management microservice with authentication, social login, profile management, addresses, and payment methods.

**Port:** 5010 (HTTP) / 5011 (HTTPS)  
**Database:** SQL Server (UserServiceDb)  
**Swagger:** https://localhost:5011/scalar/v1

## Features

### ✅ Authentication
- **Email/Password Registration** - Standard user registration with password hashing (BCrypt)
- **Login** - JWT-based authentication with access tokens (15 min) and refresh tokens (7 days)
- **Social Login** - Google and Facebook OAuth integration
- **Email Verification** - Token-based email confirmation via CommunicationService
- **Password Reset** - Forgot password with email token
- **Change Password** - Authenticated password change
- **JWT Tokens** - Access token + Refresh token with IP tracking
- **Token Revocation** - Logout and revoke all user tokens
- **Lockout** - Account lockout after 5 failed login attempts (15 min)

### 👤 User Profile
- **Personal Information** - First name, last name, date of birth, phone, profile picture
- **Preferences** - Language, currency, notification settings (email, SMS, push)
- **Prime Membership** - Amazon Prime-like membership with expiry tracking
- **Loyalty Points** - Points system for rewards
- **Account Management** - Soft delete (deactivate account)

### 📍 Address Management
- **Multiple Addresses** - Shipping, billing, or both
- **Default Address** - Mark one address as default per type
- **Full Address Fields** - Name, address lines, city, state, postal code, country, phone
- **CRUD Operations** - Create, read, update, delete addresses

### 💳 Payment Methods
- **Encrypted Card Storage** - Credit/debit card info encrypted with AES-256
- **Last 4 Digits** - Only last 4 digits stored in plain text
- **Card Brands** - Visa, Mastercard, PayPal, bank transfer
- **Expiry Tracking** - Automatic expiration detection
- **Default Payment** - Mark one method as default
- **Secure Encryption** - AES encryption for sensitive data (use Azure Key Vault in production)

## Architecture

```
UserService (Port 5011)
    ├── Authentication Layer (JWT)
    ├── Controllers
    │   ├── AuthController (Login, Register, Social Login, Password Reset)
    │   ├── UsersController (Profile Management)
    │   ├── AddressesController (Address CRUD)
    │   └── PaymentMethodsController (Payment Methods CRUD)
    ├── Services
    │   ├── TokenService (JWT Generation & Refresh Tokens)
    │   ├── CommunicationService (Email via CommunicationService)
    │   └── PaymentEncryptionService (AES Encryption)
    ├── Data Layer (EF Core + SQL Server)
    └── Identity (ASP.NET Core Identity)
```

## API Endpoints

### 🔐 Authentication (`/api/auth`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/register` | Register new user | ❌ |
| POST | `/login` | Login with email/password | ❌ |
| POST | `/social-login` | Login with Google/Facebook | ❌ |
| POST | `/refresh` | Refresh access token | ❌ |
| POST | `/logout` | Revoke refresh token | ✅ |
| GET | `/verify-email?token={token}&email={email}` | Verify email address | ❌ |
| POST | `/forgot-password` | Request password reset | ❌ |
| POST | `/reset-password` | Reset password with token | ❌ |
| POST | `/change-password` | Change password (authenticated) | ✅ |

### 👤 Users (`/api/users`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/me` | Get current user profile | ✅ |
| GET | `/{userId}` | Get user by ID | ✅ |
| PUT | `/me` | Update profile | ✅ |
| PUT | `/me/preferences` | Update preferences | ✅ |
| DELETE | `/me` | Delete account (soft) | ✅ |

### 📍 Addresses (`/api/users/{userId}/addresses`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/` | Get all addresses | ✅ |
| GET | `/{addressId}` | Get address by ID | ✅ |
| POST | `/` | Create new address | ✅ |
| PUT | `/{addressId}` | Update address | ✅ |
| DELETE | `/{addressId}` | Delete address | ✅ |
| POST | `/{addressId}/set-default` | Set as default | ✅ |

### 💳 Payment Methods (`/api/users/{userId}/payment-methods`)

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/` | Get all payment methods | ✅ |
| GET | `/{paymentMethodId}` | Get payment method | ✅ |
| POST | `/` | Add payment method | ✅ |
| DELETE | `/{paymentMethodId}` | Delete payment method | ✅ |
| POST | `/{paymentMethodId}/set-default` | Set as default | ✅ |

## Database Schema

### Tables

**Users** (ASP.NET Identity)
- Id (Guid, PK, Sequential)
- Email (Unique)
- PasswordHash
- FirstName, LastName
- DateOfBirth, ProfilePictureUrl
- PreferredLanguage, PreferredCurrency
- EmailNotificationsEnabled, SmsNotificationsEnabled, PushNotificationsEnabled
- IsPrimeMember, PrimeMembershipExpiry, LoyaltyPoints
- GoogleId, FacebookId (Social login)
- CreatedAt, LastLoginAt, IsActive

**Addresses**
- Id (Guid, PK, Sequential)
- UserId (FK → Users)
- FullName, AddressLine1, AddressLine2
- City, StateProvince, PostalCode, CountryCode
- PhoneNumber
- Type (Shipping=1, Billing=2, Both=3)
- IsDefault
- CreatedAt, UpdatedAt

**PaymentMethods**
- Id (Guid, PK, Sequential)
- UserId (FK → Users)
- Type (CreditCard=1, DebitCard=2, PayPal=3, BankTransfer=4)
- CardHolderName
- Last4Digits (plain text)
- CardBrand (Visa, Mastercard, etc.)
- ExpiryMonth, ExpiryYear
- EncryptedToken (AES encrypted card number)
- IsDefault
- CreatedAt, UpdatedAt

**RefreshTokens**
- Id (Guid, PK, Sequential)
- UserId (FK → Users)
- Token (Base64, Unique)
- ExpiresAt (7 days)
- CreatedAt, CreatedByIp
- IsRevoked, RevokedAt, RevokedByIp
- ReplacedByToken

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=UserServiceDb;..."
  },
  "Jwt": {
    "Secret": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!123",
    "Issuer": "UserService",
    "Audience": "DistributedOrderSystem",
    "ExpiresInMinutes": "15"
  },
  "Encryption": {
    "Key": "32CharacterEncryptionKey12345",  // Use Azure Key Vault in production!
    "IV": "16CharacterIV123"
  },
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id.apps.googleusercontent.com",
      "ClientSecret": "your-google-client-secret"
    },
    "Facebook": {
      "AppId": "your-facebook-app-id",
      "AppSecret": "your-facebook-app-secret"
    }
  },
  "Services": {
    "CommunicationService": {
      "BaseUrl": "https://localhost:5011"
    }
  }
}
```

### Social Login Setup

#### Google OAuth
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create project → APIs & Services → Credentials
3. Create OAuth 2.0 Client ID (Web application)
4. Add redirect URI: `https://localhost:5011/signin-google`
5. Copy ClientId and ClientSecret to appsettings.json

#### Facebook Login
1. Go to [Facebook Developers](https://developers.facebook.com/)
2. Create App → Add Facebook Login
3. Settings → Basic: Copy App ID and App Secret
4. Add redirect URI: `https://localhost:5011/signin-facebook`

## Security Features

### Password Security
- **BCrypt Hashing** - Passwords hashed with BCrypt (ASP.NET Core Identity)
- **Requirements** - Min 8 chars, uppercase, lowercase, digit
- **Lockout** - 5 failed attempts = 15-minute lockout

### Token Security
- **JWT Access Token** - Short-lived (15 minutes), stateless
- **Refresh Token** - Long-lived (7 days), stored in DB with IP tracking
- **Token Rotation** - Old refresh token revoked when new one issued
- **IP Tracking** - Track token creation and revocation by IP

### Payment Security
- **AES-256 Encryption** - Card numbers encrypted at rest
- **No CVV Storage** - CVV never saved (PCI DSS requirement)
- **Last 4 Only** - Only last 4 digits in plain text
- **Production** - ⚠️ Use Azure Key Vault or AWS KMS for encryption keys!

### API Security
- **JWT Bearer Authentication** - All protected endpoints require valid JWT
- **Authorization** - Users can only access their own data
- **CORS** - Configured for frontend origins
- **HTTPS** - Enforce HTTPS in production

## Email Integration

UserService calls **CommunicationService** for all emails:

### Email Templates
- **EmailVerification** - "Verify your email" with verification link
- **Welcome** - "Welcome to the platform" after registration
- **PasswordReset** - "Reset your password" with reset link
- **PasswordChanged** - "Your password was changed" notification

### Example Call
```csharp
await _communicationService.SendEmailVerificationAsync(
    email: "user@example.com",
    verificationUrl: "https://yourdomain.com/verify?token=xxx"
);
```

## Setup & Testing

### 1. Database Migration
```powershell
cd src/UserService
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 2. Run Service
```powershell
dotnet run --project src/UserService
```

Service starts at:
- HTTP: http://localhost:5010
- HTTPS: https://localhost:5011
- Swagger: https://localhost:5011/scalar/v1

### 3. Test Registration
```powershell
curl -X POST https://localhost:5011/api/auth/register `
  -H "Content-Type: application/json" `
  -d '{
    "email": "test@example.com",
    "password": "Test123!",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

Response:
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "test@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "sWvZ3qP8+mR4dF2hK...",
  "expiresAt": "2026-01-20T15:30:00Z"
}
```

### 4. Test Login
```powershell
curl -X POST https://localhost:5011/api/auth/login `
  -H "Content-Type: application/json" `
  -d '{
    "email": "test@example.com",
    "password": "Test123!"
  }'
```

### 5. Test Protected Endpoint
```powershell
$token = "eyJhbGciOiJIUzI1NiIs..."
curl -X GET https://localhost:5011/api/users/me `
  -H "Authorization: Bearer $token"
```

## Integration with Other Services

### GatewayBff Integration

Add user authentication to BFF:

```csharp
// GatewayBff/Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://localhost:5011";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "UserService",
            ValidateAudience = true,
            ValidAudience = "DistributedOrderSystem"
        };
    });
```

### CustomerWebsite Integration

```csharp
// Store tokens in HttpContext.Session or cookies
public async Task<IActionResult> Login(LoginViewModel model)
{
    var response = await _httpClient.PostAsJsonAsync(
        "https://localhost:5011/api/auth/login", 
        new { model.Email, model.Password }
    );
    
    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
    
    // Store tokens securely
    HttpContext.Session.SetString("AccessToken", authResponse.AccessToken);
    HttpContext.Session.SetString("RefreshToken", authResponse.RefreshToken);
    HttpContext.Session.SetString("UserId", authResponse.UserId.ToString());
    
    return RedirectToAction("Index", "Home");
}
```

### Protected API Calls

```csharp
// Add JWT token to requests
var accessToken = HttpContext.Session.GetString("AccessToken");
_httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", accessToken);

var response = await _httpClient.GetAsync("https://localhost:7000/api/cart/session123");
```

## Production Checklist

### Security
- [ ] Change JWT Secret to strong random key (64+ chars)
- [ ] Use Azure Key Vault for encryption keys
- [ ] Enable `RequireConfirmedEmail = true` in Identity
- [ ] Implement real social token validation (Google/Facebook APIs)
- [ ] Add rate limiting (ASP.NET Core Rate Limiting)
- [ ] Enable HTTPS only (disable HTTP endpoint)
- [ ] Add Content Security Policy headers
- [ ] Implement CAPTCHA for registration/login

### Database
- [ ] Use Azure SQL or AWS RDS (not localhost)
- [ ] Enable automatic backups
- [ ] Set up connection pooling
- [ ] Add database encryption at rest

### Monitoring
- [ ] Add Application Insights / Datadog
- [ ] Log all authentication events
- [ ] Monitor failed login attempts
- [ ] Alert on suspicious activity (multiple IPs, brute force)

### Performance
- [ ] Cache user profiles in Redis
- [ ] Add CDN for profile pictures
- [ ] Implement token cleanup background job
- [ ] Add database indexes (already configured)

## Roles & Permissions

Default roles created on startup:
- **Admin** - Full system access
- **Customer** - Regular user (default for new registrations)
- **Vendor** - Seller/merchant access

To assign roles programmatically:
```csharp
await _userManager.AddToRoleAsync(user, "Admin");
```

## Troubleshooting

### "Email already registered"
- Email must be unique across all users
- Check if user already exists with soft delete (`IsActive = false`)

### "Token not valid" on refresh
- Refresh token expired (7 days)
- Token was revoked (logout/change password)
- User requires re-login

### Social login not working
- Verify OAuth credentials in appsettings.json
- Check redirect URIs match exactly
- Ensure social provider is enabled
- TODO: Implement real token validation (currently accepts any token)

### Encryption error
- Ensure `Encryption:Key` is exactly 32 characters
- Ensure `Encryption:IV` is exactly 16 characters
- Use Azure Key Vault in production

## Future Enhancements

1. **Two-Factor Authentication (2FA)** - SMS/Email/Authenticator app
2. **Password History** - Prevent reusing last N passwords
3. **Session Management** - View and revoke active sessions
4. **Activity Log** - Track all user actions
5. **Account Linking** - Link multiple social accounts to one user
6. **Magic Link Login** - Passwordless email login
7. **Biometric Support** - Face ID, Touch ID integration
8. **Real Social Token Validation** - Verify Google/Facebook tokens with their APIs

## Ports Reference

| Service | Port | Purpose |
|---------|------|---------|
| UserService | 5010/5011 | Authentication & user management |
| CommunicationService | 5011 (TBD) | Email sending service |
| GatewayBff | 7000 | API Gateway with JWT validation |
| CustomerWebsite | 7001 | Frontend with session management |

---

**Documentation:** [UserService](https://localhost:5011/scalar/v1)  
**Status:** ✅ Ready for development testing  
**Production:** ⚠️ Configure social login, encryption keys, and email service before deploying
