# 📧 NotificationService - Multi-Channel Notification System

> Updated to match the real code. Several features described in previous versions of this document (template caching, rate limiting, automatic retry, granular health checks, Kafka) turn out to be **configured but not actually wired up** in the code — see the "⚠️" notes in the sections below. The database is **PostgreSQL**, not SQL Server.

## 🎯 Overview

**NotificationService** is a microservice for sending notifications across several communication channels. It supports SMS (Twilio), Email (MailKit/SMTP), Push (Firebase Cloud Messaging), and real-time in-app notifications (SignalR). The SMS/Email/Push providers are **real, working implementations** (not stubs) when configured with valid credentials — the swap to Mock only happens in the Development environment.

⚠️ This service **neither consumes nor publishes Kafka events** — there is no reference to `Confluent.Kafka` anywhere in the code. It only receives requests through its own HTTP endpoints; no other service (including GatewayBff) calls it automatically today.

## ✨ Key Features

### 📱 **Multi-Channel**
- **SMS**: Twilio (`TwilioSmsService`, real) / `MockSmsService` in Development
- **Email**: MailKit/SMTP (`MailKitEmailService`, real, with attachments and HTML) / `MockEmailService` in Development
- **Push**: Firebase Cloud Messaging (`FirebasePushService`, real) / `MockPushService` in Development — ⚠️ see the critical note below on token registration in production
- **In-App**: SignalR (`SignalRInAppNotificationService`) — always active, has no mock variant

### 🎨 **Template System**
- Templates with `{name}` placeholders replaced via a simple `string.Replace` (not a rendering engine like Razor/Handlebars)
- Validation: variables used in the template are extracted via regex and compared against those declared in the template's `Variables` JSON field
- 5 templates pre-seeded in the database (see below)
- ⚠️ **No template cache**: `NotificationTemplateService` queries the database on every call. The `Templates` section in `appsettings.json` (`EnableCaching`, `CacheExpiryMinutes`) is bound to `TemplateSettings`, but **no class injects or reads it** — it's dead configuration.

### 🔄 **Background Processing**
- Real background jobs with **Hangfire** (PostgreSQL storage) — `ScheduleNotificationAsync`/`CancelScheduledNotificationAsync`/`ExecuteScheduledNotificationAsync` are implemented and working
- ⚠️ **Retry is manual only**: `POST /api/notification/{id}/retry` resends a failed notification on the caller's explicit request. There is no automatic background retry: the `NotificationSettings` class (which declares `MaxGlobalRetries`, `RetryDelay`, `BackoffMultiplier`, `AutoCleanup`, `CleanupTime`, `RetentionDays`) **is never registered with `Configure<NotificationSettings>()` in `Program.cs`, nor injected by any class** — it's a complete but dead configuration model, not an exponential-backoff retry as the name would suggest.

### ⚡ **Performance and Scalability**
- Redis caching: used for the **unread-notification counter** and **SignalR connection tracking** (`SignalRInAppNotificationService`, `NotificationHub`) — not for caching templates or notifications themselves
- Real batch sending for SMS (`Task.WhenAll`) and Email (reused SMTP connection); for Push, batching uses `FirebaseMessaging.SendAllAsync`
- ⚠️ **Rate limiting not enforced**: `RateLimitSettings` (`SmsPerMinute`, `EmailPerMinute`, etc.) is bound in `Program.cs`, but **no code reads it to actually limit traffic**. The only limits applied are hardcoded checks in the controllers (max 100 SMS, 50 emails, 1000 notifications per bulk request)
- Health check: the `/health` endpoint is exposed, but `AddHealthChecks()` is called **with no checks registered** (the Postgres and Redis checks are commented out in the source with `// .AddRedis(...)` and a note explaining that `AddDbContextCheck` should be used instead of what's there) — the endpoint always responds "Healthy" regardless of the actual state of the database or Redis

## 🏗️ Architecture

```
NotificationService/
├── Controllers/
│   ├── NotificationChannelControllers.cs   # SmsController (/api/sms) + EmailController (/api/email)
│   ├── NotificationController.cs           # /api/notification — send via template, direct, bulk, schedule, stats, retry
│   └── PushAndInAppControllers.cs          # PushController (/api/push) + InAppNotificationController (/api/inappnotification)
├── Data/
│   └── NotificationContext.cs              # EF Core (Npgsql): Notifications, NotificationTemplates,
│                                            # NotificationPreferences, NotificationLogs — NOTHING ELSE
├── Models/
│   ├── NotificationModels.cs               # EF Core entities
│   ├── NotificationRequests.cs             # Request DTOs
│   └── NotificationResponses.cs            # Response DTOs
├── Services/
│   ├── INotificationServices.cs            # Interfaces
│   └── Implementations/
│       ├── SmsService.cs                   # TwilioSmsService + MockSmsService
│       ├── EmailService.cs                 # MailKitEmailService + MockEmailService
│       ├── PushService.cs                  # FirebasePushService + MockPushService
│       ├── InAppNotificationService.cs     # SignalRInAppNotificationService + NotificationHub (SignalR Hub)
│       ├── NotificationTemplateService.cs  # Templates: fetch/render/validate/CRUD
│       └── NotificationService.cs          # Main orchestrator (routing, Hangfire, bulk, retry)
├── Configuration/
│   └── NotificationSettings.cs             # SmsSettings, EmailSettings, PushNotificationSettings,
│                                            # NotificationSettings (⚠️ never bound), RedisSettings,
│                                            # TemplateSettings (⚠️ never used), WebhookSettings (⚠️ never bound),
│                                            # RateLimitSettings (⚠️ bound but never enforced)
└── Program.cs                              # Bootstrap: EF Core+Npgsql, Hangfire, SignalR, mock/real by env
```

## 📋 Supported Providers

| Channel | Provider | Real status | Environment |
|--------|----------|-------------|----------|
| **SMS** | Twilio (`TwilioSmsService`) | Real and working with valid credentials | Production |
| **SMS** | `MockSmsService` | Logs to console, simulates success | Development |
| **Email** | MailKit/SMTP (`MailKitEmailService`) | Real, with attachments, SMTP connection pooling | Production |
| **Email** | `MockEmailService` | Logs to console | Development |
| **Push** | Firebase FCM (`FirebasePushService`) | Real single/topic sending; ⚠️ **`SendToUserAsync`/`RegisterDeviceTokenAsync` are broken in production** (see below) | Production |
| **Push** | `MockPushService` | Tokens kept in an in-memory dictionary, always works | Development |
| **Real-time** | SignalR (`NotificationHub` at `/hubs/notifications`) | Real, single instance (no Redis backplane — see below) | Always active |

### ⚠️ Critical bug: push token registration in production

`FirebasePushService.RegisterDeviceTokenAsync` and `GetUserDeviceTokensAsync` run **raw SQL** against a `UserDeviceTokens` table:
```csharp
await _context.Database.ExecuteSqlAsync(
    $@"INSERT INTO UserDeviceTokens (UserId, DeviceToken, Platform, RegisteredAt, IsActive)
       VALUES ({userId}, {deviceToken}, {platform}, {DateTime.UtcNow}, 1)", cancellationToken);
```
This table **does not exist in the EF Core model** — `NotificationContext` has no corresponding `DbSet`, there is no mapping in `OnModelCreating`, and the service doesn't use EF migrations (it calls `EnsureCreated()`, which only creates tables that actually exist in the model). As a result, in any non-Development environment (where `FirebasePushService` is injected instead of the mock), `POST /api/push/register-token` and `POST /api/push/send-to-user` **will fail with a SQL error** ("relation UserDeviceTokens does not exist") on the very first call. Only the Mock path (Development) works, because it keeps tokens in an in-memory `Dictionary<string, List<string>>`.

## 🔧 Configuration

### **appsettings.Development.json (real content)**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=DistributedOrderSystemNotifications;Username=postgres;Password=YourStrong_Password123;",
    "Redis": "localhost:6379"
  },
  "Sms": { "AccountSid": "...", "AuthToken": "...", "FromPhoneNumber": "+1234567890", "MaxRetries": 3, "TimeoutSeconds": 30 },
  "Email": { "Host": "smtp.gmail.com", "Port": 587, "UseSsl": false, "Username": "...", "Password": "...", "FromEmail": "...", "FromName": "Distributed Order System" },
  "PushNotification": { "ServiceAccountJson": "{ ... Firebase service account ... }" },
  "Redis": { "ConnectionString": "localhost:6379", "Database": 0, "KeyPrefix": "notifications:", "DefaultExpiry": "01:00:00" },
  "Templates": { "EnableCaching": true, "CacheExpiryMinutes": 60, "MaxTemplateSize": 1048576 },
  "RateLimit": { "SmsPerMinute": 100, "EmailPerMinute": 50, "PushPerMinute": 1000, "BurstLimit": 200 },
  "Hangfire": { "Dashboard": { "Enabled": true, "PathMatch": "/hangfire" }, "RetryAttempts": 3, "BackgroundJobExpiration": "24:00:00" }
}
```
⚠️ The `Templates`, `RateLimit`, `Hangfire:RetryAttempts`/`BackgroundJobExpiration` sections are present in the file, but — as explained above — only `Templates`/`RateLimit` are actually bound (to `TemplateSettings`/`RateLimitSettings`), and neither of them is then consulted by application code. The `Hangfire` sub-section doesn't even have a binding class: Hangfire itself is configured directly in `Program.cs` with hardcoded parameters, not from this section.

### **Program.cs — real wiring**

```csharp
builder.Services.AddDbContext<NotificationContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));

builder.Services.AddHangfire(cfg => cfg
    .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));
builder.Services.AddHangfireServer();

builder.Services.AddSignalR();
// Note in the code: "AddStackExchangeRedis extension is not available, Redis can be configured separately"
// → no Redis backplane for SignalR: if this service scales to multiple instances, users connected
//   to different instances will NOT receive notifications sent from another instance.

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<ISmsService, MockSmsService>();
    builder.Services.AddScoped<IEmailService, MockEmailService>();
    builder.Services.AddScoped<IPushService, MockPushService>();
}
else
{
    builder.Services.AddScoped<ISmsService, TwilioSmsService>();
    builder.Services.AddScoped<IEmailService, MailKitEmailService>();
    builder.Services.AddScoped<IPushService, FirebasePushService>();
}

builder.Services.AddHealthChecks(); // no checks added

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() } // ⚠️ Authorize() always returns true
});

app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/health");
```

⚠️ **`HangfireAuthorizationFilter.Authorize` always returns `true`**, with a comment in the code ("In development, allow free access — always true for now") that doesn't distinguish Development from Production: the Hangfire dashboard (background jobs, with data for every scheduled notification) is **accessible without authentication in any environment** until this filter is fixed.

## 📬 API Endpoints (real routes)

### SMS (`/api/sms`)
`POST /send`, `POST /send-bulk` (max 100), `GET /status/{messageId}`, `GET /validate?phoneNumber=...`

### Email (`/api/email`)
`POST /send`, `POST /send-bulk` (max 50), `POST /send-with-attachments`, `GET /validate?email=...`

### Push (`/api/push`)
`POST /send`, `POST /send-to-user` (⚠️ broken in production, see above), `POST /send-to-topic`, `POST /register-token` (⚠️ broken in production)

### In-App (`/api/inappnotification`)
`POST /send`, `GET /unread/{userId}?limit=50`, `POST /{notificationId}/mark-read`, `POST /mark-all-read/{userId}`

### General notifications (`/api/notification`)
`POST /send-template`, `POST /send-direct`, `POST /send-bulk` (max 1000), `POST /schedule` (Hangfire), `DELETE /schedule/{id}`, `GET /{id}/status`, `GET /user/{userId}?page=&pageSize=&type=&status=`, `GET /stats?userId=&fromDate=&toDate=` (max period 1 year), `POST /{id}/retry`

Example send with template (unchanged from the previous version, matches the real code):
```http
POST /api/notification/send-template
Content-Type: application/json
{
    "templateName": "order_confirmation",
    "recipient": "customer@example.com",
    "type": "Email",
    "variables": { "customerName": "Mario Rossi", "orderId": "12345", "orderDetails": "2x Margherita Pizza", "total": "€25.00", "companyName": "Mario's Pizzeria" }
}
```

## 🔄 SignalR Hub for Real-Time Notifications

Behavior unchanged from the previous version — client connection, `JoinUserGroup`, the `ReceiveNotification`/`UnreadCountUpdate`/`ReceiveBroadcast` events are real and implemented in `NotificationHub`/`SignalRInAppNotificationService`. The "unread" counter and connection tracking (`user_connections_{userId}` in Redis) are genuinely maintained in Redis — but, as noted, this is an application-level use of Redis, not SignalR's own scale-out mechanism (which would require `AddStackExchangeRedis()`, not present).

## 🎨 Template System

### Predefined Templates (seeded in `NotificationContext.OnModelCreating` → `SeedTemplates`)
1. **order_confirmation** (Email)
2. **order_shipped** (SMS)
3. **payment_reminder** (Email)
4. **welcome_user** (Push)
5. **system_maintenance** (InApp)

These 5 records are created as EF Core seed data (`HasData`), so they exist in the database from the very first `EnsureCreated()`/migration — they don't require a manual initialization step.

### Real rendering
```csharp
private static string ReplaceVariables(string template, Dictionary<string, string> variables)
{
    var result = template;
    foreach (var variable in variables)
        result = result.Replace("{" + variable.Key + "}", variable.Value ?? string.Empty);
    return result;
}
```
No HTML sanitization of the variables substituted into `HtmlTemplate` — anyone who submits a value containing markup will see it inserted as-is into the final HTML content.

## 🔍 Monitoring and Diagnostics

### Health Check — real behavior
```http
GET /health
```
⚠️ Always responds with the aggregate status of **zero registered checks** (typically `200 OK` / `"Healthy"`, the default from `AddHealthChecks()` with no checks), regardless of the actual reachability of PostgreSQL, Redis, or external providers. The response body with `database`/`redis`/`sms_provider`/`email_provider` sub-keys shown in previous versions of this document **does not match what the code produces today**.

### Hangfire Dashboard
`http://localhost:5246/hangfire` (the service's real port; not 5000 as stated in previous versions). ⚠️ No real authentication, see the note above.

### Notification Statistics
`GET /api/notification/stats?userId=...&fromDate=...&toDate=...` — implemented, computes counts by status/type/priority directly from the database on every call (no cache).

## 🔐 Security — real state, not aspirational

- ⚠️ **Rate limiting: configured but not enforced** (see above) — doesn't protect against spam despite the `RateLimit` section in `appsettings`.
- ⚠️ **Hangfire dashboard with no real authentication.**
- ✅ Basic input validation in the controllers (regex for phone/email, `ModelState.IsValid`, hardcoded limits on bulk sizes).
- No application-level encryption of provider credentials beyond what `appsettings`/environment variables offer on their own — there's no secrets-management layer (Key Vault, etc.) in this service.
- CORS: default policy `AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()` — permissive, not restricted to known domains.

## 🧪 Testing

### Mock Services
In the `Development` environment, `MockSmsService`/`MockEmailService`/`MockPushService` are used automatically — these log to console and always simulate success (except for obviously invalid input, e.g. a phone number that fails the regex). They don't simulate realistic provider failures (timeouts, real provider rate limits, etc.).

No automated test project exists for this service (consistent with the state of the whole solution).

## 💡 Usage Examples

The "Order Confirmation" and "Shipping Notification" scenario examples from the previous version remain valid as *direct calls to the interfaces* (`ISmsService`, `INotificationService`, `IInAppNotificationService`) — no external service (OrderService included) invokes them automatically today: this would be code to add in OrderService or a dedicated consumer, not something that already happens when an order is created.

---

**NotificationService** offers multi-channel infrastructure with real providers (Twilio/MailKit/Firebase) and real background jobs (Hangfire), but with several "resilience" features (automatic retry, rate limiting, template caching, granular health checks) present only as configuration that isn't wired to the code, plus one concrete bug that breaks push token registration in production. It should be treated as a working service for the tested manual/synchronous paths (direct SMS/Email/In-App sending, Hangfire-scheduled notifications), not yet as a "production-ready" notification platform in the sense of automatic resilience.
