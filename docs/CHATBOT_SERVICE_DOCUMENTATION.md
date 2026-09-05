# 🤖 CHATBOT SERVICE DOCUMENTATION

> **Updated to reflect the real code.** Previous versions of this document (and of `CHATBOT_WIDGET_DOCUMENTATION.md`) described a "fully implemented and operational" system with a real Microsoft DialoGPT-small model via ONNX Runtime, CUDA GPU acceleration, a working fine-tuning framework, and measured accuracy metrics (94.2%, etc.). **None of these claims match the code present in the repository.** This document replaces those descriptions with what the code actually does, including some concrete breakages (endpoints that don't respond, unregistered services) found by reading `Program.cs` and the controllers.

## Overview

**ChatbotService** is a microservice that exposes a complete chatbot infrastructure (controllers, JWT authentication, admin dashboard, embeddable JS widget, fine-tuning framework) — but **the AI part is entirely simulated** and **the main chat endpoint doesn't work** due to an unresolved dependency-injection problem. The service starts up and responds on many routes, but the chatbot's "brain," as described in previous versions of this document, doesn't exist in the code.

## ⚠️ What Works and What Doesn't — Summary

| Component | Real status |
|---|---|
| `POST /api/chat/message`, `GET /api/chat/history/{sessionId}`, `POST /api/chat/session`, `DELETE /api/chat/session/{sessionId}`, `GET /api/chat/intents`, `POST /api/chat/feedback` | ❌ **Not working**: `ChatController` requires `IChatbotService` in its constructor, but **no class in the repository implements this interface**, and its registration in `Program.cs` is commented out (`// builder.Services.AddScoped<IChatbotService, Services.ChatbotService>();`). Every request to these endpoints fails with a DI resolution error. |
| `POST /api/chat/login`, `/api/chat/register`, `GET /api/chat/profile` | ⚠️ Respond, but `AuthenticationService` is purely a demo: hardcoded credentials (`admin@example.com`/`password123`, `user@example.com`/`password123`), no database check, `RegisterAsync` persists nothing. |
| NLP engine / DialoGPT | ❌ **Fully simulated** — see the dedicated section below. No ONNX inference ever happens. |
| Fine-tuning (`FineTuningController`, `AiDashboardController`) | ❌ **Fully simulated** — "training" is a client-side JavaScript timer using `Math.random()`; the server-side endpoints just do `Task.Delay(...)` and return `success`. |
| Admin dashboard at the root (`/` → `/api/admin`) | ❌ **Broken route**: the redirect points to `/api/admin`, but the only controller with dashboard-redirect logic (`SimpleAdminController`) uses `[Route("api/[controller]")]`, which resolves to `api/SimpleAdmin`, not `api/admin`. The redirect results in a 404. |
| `GET /api/aidashboard`, `GET /api/finetuning` | ✅ Respond (the routes match), but only show the simulated HTML interface described above. |
| Chat Widget (static files, `ChatWidgetController`) | ✅ **File serving is genuinely implemented** — the files really exist (see the Widget section), but the widget itself, when it tries to chat, calls endpoints that are either broken (see above) or don't exist on the GatewayBff side. |
| `ServiceIntegrationService` (calls to other microservices) | ⚠️ Partially real: `GetOrderByIdAsync`/`GetUserOrdersAsync`/`SearchProductsAsync` call real GatewayBff routes (`/api/queries/orders`, `/api/queries/catalog`); `CancelOrderAsync` and `GetPaymentInfoAsync` call routes that **don't exist** on GatewayBff (`/api/commands/orders/{id}/cancel`, `/api/queries/payments/order/{id}`). None of this is invoked by any real chat path anyway, since `ChatController` is unreachable. |
| JWT authentication (middleware) | ✅ Correctly configured and working as a mechanism — it's the logic *behind* login that's a demo, not the JWT middleware itself. |
| Persistence (PostgreSQL `ChatbotDb_Dev`, Redis) | ✅ Real, correct configuration (see Configuration section). |

## Technical Architecture

### Real Technology Stack
```
- .NET 9.0
- Entity Framework Core 9.0.0 + Npgsql (PostgreSQL, not SQL Server)
- Microsoft.ML 4.0.0, Microsoft.ML.OnnxRuntime 1.19.2, Microsoft.ML.OnnxRuntime.Gpu 1.19.2,
  Microsoft.ML.Tokenizers, Microsoft.ML.TensorFlow — packages referenced in the .csproj,
  but ONNX Runtime's InferenceSession is NEVER instantiated in the code (see below)
- StackExchange.Redis
- JWT Bearer Authentication (works as middleware)
- Scalar/OpenAPI for documentation
```

### Project Structure (real)
```
ChatbotService/
├── Configuration/          # AuthSettings, ModelSettings, ServiceUrlsSettings, FineTuningSettings
├── Controllers/
│   ├── ChatController.cs          # real routes, but IChatbotService isn't registered → 500/DI error
│   ├── ChatWidgetController.cs    # serves the widget's static files — working
│   ├── SimpleAdminController.cs   # redirects to /api/aidashboard, health check
│   ├── AiDashboardController.cs   # HTML dashboard with simulated data
│   └── FineTuningController.cs    # fine-tuning HTML dashboard with simulated data
├── Data/
│   ├── ChatContext.cs              # EF Core / Npgsql
│   └── Entities/{ChatMessage,ChatSession,TrainingData}.cs
├── Models/                 # ChatModels, IntentModels, AuthModels
├── Models/Downloaded/DialoGPT-small/  # config.json (43 bytes), tokenizer.json (35 bytes),
│                                        # model.onnx (33 bytes) — all text placeholders, not a real model
├── Services/
│   ├── AdvancedNLPService.cs       # registered as INLPService — simulates DialoGPT, see below
│   ├── LightweightNLPService.cs    # a more honest alternative implementation, NEVER registered in DI
│   ├── AuthenticationService.cs    # demo, hardcoded credentials
│   ├── ServiceIntegrationService.cs # HTTP calls to other services, partially real
│   ├── FineTuningService.cs        # backs the simulated fine-tuning controllers
│   └── ModelInitializationService.cs # BackgroundService that "downloads" the model at startup
└── Program.cs
```

## Real Configuration

### appsettings.json (real content, not what previous versions of the document showed)
```json
{
  "ConnectionStrings": {
    "ChatbotDb": "Host=localhost;Port=5432;Database=ChatbotDb_Dev;Username=postgres;Password=YourStrong_Password123;",
    "Redis": "localhost:6379"
  },
  "ServiceUrls": {
    "OrderService": "http://localhost:5003",
    "ProductService": "http://localhost:5198",
    "InventoryService": "http://localhost:5051",
    "PaymentService": "http://localhost:5034",
    "GatewayBff": "http://localhost:5189"
  },
  "ModelSettings": {
    "BaseModelPath": "./Models/Downloaded",
    "FineTunedModelPath": "./Models/FineTuned/chatbot_model.onnx",
    "EmbeddingModelName": "sentence-transformers/all-MiniLM-L6-v2",
    "MaxTokens": 150,
    "Temperature": 0.7,
    "TopP": 0.9
  },
  "Auth": {
    "Issuer": "ChatbotService",
    "Audience": "DistributedOrderSystem",
    "SecretKey": "your-super-secret-key-here-must-be-at-least-256-bits",
    "TokenExpirationMinutes": 120
  },
  "FineTuning": {
    "TrainingDataPath": "./Data/TrainingData",
    "MaxTrainingIterations": 1000,
    "LearningRate": 0.001,
    "BatchSize": 16,
    "ValidationSplit": 0.2
  }
}
```
Note: `EmbeddingModelName: "sentence-transformers/all-MiniLM-L6-v2"` is present in configuration but **not used anywhere in the code** — embeddings are generated as random vectors (see below), not with that model.

## API Endpoints (real routes verified in the controllers)

### 🤖 Chat API — `ChatController`, base route `/api/chat`
All routes exist and are reachable by the router, but **the ones that depend on `IChatbotService` fail at runtime**:
- `POST /api/chat/message` ❌ (DI can't be resolved)
- `GET /api/chat/history/{sessionId}` ❌
- `POST /api/chat/session` ❌
- `DELETE /api/chat/session/{sessionId}` ❌
- `GET /api/chat/intents` ❌
- `POST /api/chat/feedback` ❌
- `POST /api/chat/login` ⚠️ works, but it's demo authentication (see above)
- `POST /api/chat/register` ⚠️ works, but doesn't persist the user
- `GET /api/chat/profile` ⚠️ works if authenticated, but always returns the same fake profile
- `GET /api/chat/interface` ✅ returns a simple HTML test page that calls `POST /api/chat/message` — so even this page never actually gets a real response from the bot

### 🎨 Widget API — `ChatWidgetController`, base route `/api/chatwidget`
All of these routes are real and working as file/config *serving* (they don't generate AI responses):
- `GET /api/chatwidget/chat-widget.min.js` ✅ serves `wwwroot/chat-widget/dist/chat-widget.min.js`
- `GET /api/chatwidget/demo` ✅ serves `wwwroot/chat-widget/demo.html`
- `GET /api/chatwidget/config` ✅ generates a dynamic configuration script
- `GET /api/chatwidget/health` ✅
- `POST /api/chatwidget/integration-snippet` ✅ generates an HTML/JS snippet
- `GET /api/chatwidget/stats` ⚠️ returns numbers **generated with `Random.Shared.Next(...)`** on every call (not real statistics)

### 📊 Admin/Dashboard
- `GET /` → redirects to `/api/admin` → ❌ **404**, because no controller exposes that exact route
- `GET /api/simpleadmin` (or `/api/simpleadmin/dashboard`) → redirects to `/api/aidashboard` ✅
- `GET /api/simpleadmin/health` ✅
- `GET /api/aidashboard` ✅ HTML with Bootstrap, simulated data
- `GET /api/aidashboard/model-status` ✅ always returns `parameters: "117M"`, `language: "Italian/English"` regardless of any real model
- `POST /api/aidashboard/download-model` ⚠️ calls `AdvancedNLPService.DownloadAndLoadModelAsync()`, which writes the placeholder files described above
- `GET /api/aidashboard/statistics` ⚠️ counts real rows in `ChatSessions`, but `avgResponseTime` is `Random.Shared.Next(80, 200)` and `modelAccuracy`/`uptime` are hardcoded strings (`"95.2%"`, `"99.9%"`)
- `GET /api/finetuning`, `/api/finetuning/dashboard` ✅ HTML dashboard, simulated data
- `POST /api/finetuning/download-model` ⚠️ `Task.Delay(2000)` then `success` — doesn't download anything real
- `POST /api/finetuning/start-training` ⚠️ `Task.Delay(1000)` then `started` — doesn't start any training; the progress shown in the browser is generated client-side with `setInterval` + `Math.random()`
- `POST /api/finetuning/training-data` ✅ unlike the others, this one **really writes** a row to the PostgreSQL `TrainingData` table
- `GET /api/finetuning/stats` ⚠️ counts real rows (`ChatMessages`, `ChatSessions`, `TrainingData`) but `accuracy: "94.2%"` is hardcoded

## The "AI" Engine — What Actually Happens

`Program.cs` registers `AdvancedNLPService` as `INLPService` (not `LightweightNLPService`, which does exist but is never used). The name is misleading:

1. **`DownloadAndLoadModelAsync`** looks for `Models/Downloaded/DialoGPT-small/model.onnx`; if it doesn't exist, it calls `DownloadModelFiles`, which **writes three placeholder text files** (`"# DialoGPT ONNX Model Placeholder"`, a 43-byte JSON, a 35-byte JSON) — it doesn't download anything from Hugging Face despite the name and log messages suggesting otherwise. These are exactly the ~30-45-byte files present today in `Models/Downloaded/DialoGPT-small/`.
2. **`LoadModelAsync`** logs "🎮 GPU (CUDA) acceleration enabled" if `sessionOptions.AppendExecutionProvider_CUDA(0)` doesn't throw an exception, then does `await Task.Delay(500)` and sets `_isModelLoaded = true`. **The line that would create the real `InferenceSession` is commented out** (`// _inferenceSession = new InferenceSession(modelPath, sessionOptions);`) — `_inferenceSession` stays `null` for the entire lifetime of the process.
3. **`GenerateResponseAsync`** checks `if (_isModelLoaded && _inferenceSession != null)`: since `_inferenceSession` is always `null`, this condition is **always false**, so the "DialoGPT" branch (which is itself just a `switch` over intents with fixed strings, not a real generative model) never runs. Every reply comes from `GenerateTemplateResponse`, which picks a random string from a small dictionary of Italian per-intent templates (`_responseTemplates`).
4. **Intent classification**: `ClassifyIntentAsync` just does `message.ToLower().Contains(pattern)` over lists of Italian/English keywords (e.g. `"ciao"` [hello], `"ordine"` [order], `"cancella"` [cancel]) — no ML model involved.
5. **`GetEmbeddingAsync`** returns a `float[384]` array filled with **random numbers** generated from `new Random(message.GetHashCode())` — it's not a semantic embedding of any kind, and indeed it's never compared/used for similarity anywhere else in the reachable code path (the cosine-similarity comparison only exists in `LightweightNLPService`, which isn't registered).

In summary: in the code path that's actually executed, the "chatbot" is a simple keyword matcher with templated replies — not necessarily wrong as a minimal architecture, but a long way from "Microsoft DialoGPT-small with CUDA GPU acceleration" as described in previous versions of this document. And since `ChatController` is unreachable due to the DI problem, **not even this keyword engine is ever invoked in practice through the public API** — it would only be reachable if someone registered an `IChatbotService` implementation that calls it internally, which doesn't exist today.

## Authentication — What's Real

The JWT Bearer **middleware** in `Program.cs` is correctly configured and works as expected (validates issuer, audience, signature, expiry). What isn't real is **the source of identities**:
- `AuthenticationService.ValidateCredentialsAsync` only compares against two hardcoded email/password pairs in the code.
- `RegisterAsync` generates a token for whatever email/name is submitted, without writing anything to the database.
- `GetUserProfileAsync` always returns the same fake profile (`demo_user`), ignoring the user id passed in.

Treat this as a development fixture, not as an authentication system to expose.

## Integration with Other Microservices — `ServiceIntegrationService`

This is the component closest to a real integration:
```csharp
// Genuinely work (routes exist on GatewayBff)
GetOrderByIdAsync   → GET {GatewayBff}/api/queries/orders/{orderId}
GetUserOrdersAsync  → GET {GatewayBff}/api/queries/orders
SearchProductsAsync → GET {GatewayBff}/api/queries/catalog (then filtered client-side)
GetInventoryAsync   → GET {InventoryService}/api/inventory/{productId}

// Don't work (routes don't exist on GatewayBff)
CancelOrderAsync    → PUT {GatewayBff}/api/commands/orders/{orderId}/cancel   ⚠️ 404
GetPaymentInfoAsync → GET {GatewayBff}/api/queries/payments/order/{orderId}  ⚠️ 404
```
None of these methods, however, are called from any HTTP path that's reachable today, since the controller that would orchestrate the conversation (`ChatController`, via an `IChatbotService` that's never implemented) doesn't work.

## How to Actually Make This Service Work (guidance, not current status)

If you want to bring this service to the state described in previous versions of the documentation, the minimum work is:
1. Write a class that implements `IChatbotService` and register it in `Program.cs` (uncomment the line) — without this, the entire Chat API stays dead.
2. Consciously decide whether to invest in a real ONNX/DialoGPT engine (download real weights, instantiate `InferenceSession`, actually tokenize) or accept the current keyword-matching engine as the intended implementation — but then the documentation and the logs (`"🤖 Downloading DialoGPT-small model from Hugging Face"`) should be aligned with that choice.
3. Fix the `/` → `/api/admin` redirect to `/api/simpleadmin` (or add an explicit `[Route("api/admin")]` to `SimpleAdminController`).
4. Replace `AuthenticationService` with real validation (or wire it to UserService, which already exists in the system with ASP.NET Identity).
5. Implement `CancelOrder`/`GetPaymentInfo` on GatewayBff, or remove those methods from `ServiceIntegrationService` if they aren't needed.

## Conclusion

ChatbotService compiles, starts up, and reliably serves HTML pages and static files. The part that would make it an actual chatbot — the chat endpoint and the NLP engine — is either unreachable or doesn't do what it claims to do. It should be considered advanced scaffolding for a chatbot, not a working chatbot.
