# 🤖 ChatbotService - Chat Widget

> **Update (this revision): the widget is now wired up and working end-to-end.** Everything below the "🔧 Fixed (see below)" markers described a genuinely broken state as of when this document was last written — kept here because the *reasoning* about the architecture is still useful for understanding the code, but the specific "doesn't work" claims are now out of date. See "✅ Current State" at the bottom for what changed and why.
>
> Earlier still: the *previous* version of this document (v2.0.0, "December 16, 2025") described a production AI system with Microsoft DialoGPT-small via ONNX Runtime, measured NVIDIA GPU acceleration, a complete fine-tuning framework with real metrics (89.4% accuracy, 3.2GB GPU memory, etc.), and SQL Server as the database. **None of those claims matched the code in the repository.** For a detailed breakdown of what's real vs. simulated in the NLP engine, authentication, and fine-tuning, see `CHATBOT_SERVICE_DOCUMENTATION.md` — this document focuses only on the **chat widget**.

## 🌟 Overview

The **Chat Widget** is a standalone JavaScript script, served by `ChatbotService`, meant to be embedded in any web page. Unlike the NLP engine and fine-tuning (both simulated, see `CHATBOT_SERVICE_DOCUMENTATION.md`), the widget **genuinely exists as an artifact**: TypeScript source, a minified build, an HTML demo, and a README are all present and substantial, not placeholders.

### Files Genuinely Present (verified)
```
src/ChatbotService/wwwroot/chat-widget/
├── package.json                    # metadata only, no build step
├── demo.html                       # ~540 lines — real interactive demo page
├── README.md                       # usage docs
└── dist/
    └── chat-widget.min.js          # ~840 lines — the ACTUAL widget served by ChatWidgetController
```
🔧 **Fixed:** this used to also list `angular.json` and a `src/` folder (`chat-widget.component.ts`, `index.ts`) — an Angular-based rewrite of the widget that was never finished and never wired into any build producing `dist/chat-widget.min.js` (its own `package.json` build script, `ng build --prod`, would have produced a completely different file format than the small hand-written vanilla-JS bundle actually served). It sat unused alongside the real widget and was deleted as dead code, since keeping it around only invited a future reader to assume it was the source of truth for `dist/chat-widget.min.js` when it never was.

`chat-widget.min.js` isn't a skeleton file: it genuinely implements theming, positioning, client-side session handling, etc. The problem was never that the widget is fake — it's that **the backend it tries to talk to didn't respond correctly** (see below for what was wrong and how it was fixed).

## 🏗️ Real Architecture

```
Chat Widget (JS, wwwroot/chat-widget/dist/chat-widget.min.js)
    │
    │  fetch to baseUrl = useBffRouting ? config.bffBaseUrl : config.chatbotServiceUrl
    │  (default in source: useBffRouting: true, bffBaseUrl: '/api/gateway/chat')
    ▼
┌────────────────────────────────────┬──────────────────────────────────────┐
│ BFF mode (default, used by ShopVerse)│ Direct mode                          │
│ calls {bffBaseUrl}/message          │ calls {chatbotServiceUrl}/message     │
│ → GatewayBff's ChatBffController    │ → ChatbotService's ChatController     │
│ → forwards to ChatbotService's      │   directly (no proxy hop)             │
│   /api/chat/message                 │                                        │
│ ✅ works                            │ ✅ works, but bypasses the BFF        │
└──────────────────────────────────────┴──────────────────────────────────────┘
```

🔧 **Fixed — two independent bugs, both now resolved:**

1. **GatewayBff had no `api/gateway/chat` route at all.** Any BFF-mode request 404'd regardless of what ChatbotService did. Fixed by adding `ChatBffController` (in `src/GatewayBff/Controllers/ChatBffController.cs`), a thin proxy that forwards `GET health` and `POST message` to ChatbotService, plus a named `"ChatbotService"` `HttpClient` registered in `GatewayBff/Program.cs`.
2. **The widget itself called the wrong path.** `sendToBackend()` in `chat-widget.min.js` POSTed to `{baseUrl}/analyze` — an endpoint that was never implemented anywhere (not on ChatbotService, not on GatewayBff). The real chat endpoint is `POST .../message` (matches `ChatController`'s `[HttpPost("message")]` in ChatbotService). Fixed by changing that one literal string in `chat-widget.min.js`.

Separately, `IChatbotService` (which `ChatController` depends on) is now implemented by `PythonAgentChatbotService` (`src/ChatbotService/Services/PythonAgentChatbotService.cs`), which bridges to a Python `AgentService` (LangGraph + Anthropic) over HTTP — see `CHATBOT_SERVICE_DOCUMENTATION.md` for that side of the picture if it's still marked as unregistered there.

The widget's **static-serving** routes (script, demo, config, integration snippet) always worked correctly and were never affected by either bug above.

## 🎨 What Actually Works

### Serving the Widget — `ChatWidgetController` (base route `/api/chatwidget`)
All of these routes are real and were tested against the controller's code:

| Route | Works | Notes |
|---|---|---|
| `GET /api/chatwidget/chat-widget.min.js` | ✅ | Reads and serves `dist/chat-widget.min.js` from disk, with a 1-hour cache |
| `GET /api/chatwidget/demo` | ✅ | Serves `demo.html` from disk |
| `GET /api/chatwidget/config` | ✅ | Dynamically generates a configuration script from query params |
| `GET /api/chatwidget/health` | ✅ | Static response `{status: "healthy", ...}` |
| `POST /api/chatwidget/integration-snippet` | ✅ | Generates a real HTML/JS snippet to copy |
| `GET /api/chatwidget/stats` | ⚠️ | Responds, but every number (`totalDownloads`, `activeInstances`, etc.) is generated with `Random.Shared.Next(...)` on every call — not collected statistics |

### Customization (real, read from the component's source)
The component genuinely supports theme, colors, position, quick messages, and similar via `window.chatWidgetConfig`, as in the example below (unchanged from the previous version of the document, because this part is verified in the code):
```javascript
window.chatWidgetConfig = {
    theme: 'dark',                     // light | dark | auto
    primaryColor: '#9f7aea',
    secondaryColor: '#2d3748',
    borderRadius: '16px',
    position: 'bottom-right',
    autoOpen: false,
    showTypingIndicator: true,
    enableSoundNotifications: true,
    maxMessages: 150,
    botName: 'Assistente AI',          // "AI Assistant" — the real default bot name string in source
    welcomeMessage: 'Ciao! Come posso aiutarti oggi?'  // "Hi! How can I help you today?" — real default welcome string in source
};
```

### Frontend Integration

**BFF mode** (used by ShopVerse — see `_Layout.cshtml` in `CustomerWebsite`):
```html
<script>
  // Must run BEFORE the widget script tag below — the widget auto-starts
  // itself the instant it loads if window.chatWidgetConfig is already set.
  window.chatWidgetConfig = {
    useBffRouting: true,
    bffBaseUrl: 'http://localhost:5189/api/gateway/chat', // GatewayBff
    theme: 'light'
  };
</script>
<script src="http://localhost:5055/api/chatwidget/chat-widget.min.js"></script>
```
Note the absolute URLs: ShopVerse (port 5100) is a different origin than GatewayBff (5189) and ChatbotService (5055), so relative paths like `/api/gateway/chat` would resolve against ShopVerse's own server instead. This works because loading a `<script src>` cross-origin is not subject to CORS (only `fetch`/`XHR` calls are), and GatewayBff's CORS policy allows any origin (`Program.cs` → `AddCors("AllowFrontend")`).

**Direct mode** (skips the BFF, talks to ChatbotService directly — only use this for a page outside `DistributedOrderSystem` that doesn't go through GatewayBff at all):
```html
<script src="https://your-chatbot-service.com/api/chatwidget/chat-widget.min.js"></script>
<script>
  window.chatWidgetConfig = {
    useBffRouting: false,
    chatbotServiceUrl: 'https://your-chatbot-service.com/api/chat',
    theme: 'dark'
  };
</script>
```
⚠️ ChatbotService's CORS policy (`Program.cs`) only allow-lists `localhost:4200` (Angular) and `localhost:5189`/`7189` (GatewayBff) — an arbitrary external origin would need to be added there first.

## 📚 API Reference (verified routes)

### `GET /api/chatwidget/chat-widget.min.js`
Responds `application/javascript` with the real bundle content.

### `GET /api/chatwidget/config`
Supported query string: `theme`, `primaryColor`, `position`, `useBffRouting` (among others — see `WidgetConfigRequest` in the controller for the full list). Response: a JS script that sets `window.chatWidgetConfig`.

### `GET /api/chatwidget/demo`
Serves the real demo page — useful for visually checking the widget, keeping in mind that sending a message won't get a valid response from the backend in the current state.

## 🔍 Troubleshooting

### The widget doesn't get a response when I send a message
As of this revision this should work end-to-end (see "🔧 Fixed" above). If it still doesn't, check these in order:
1. Is `ChatbotService` running? `curl -i http://localhost:5055/health`
2. Is `AgentService` (the Python/LangGraph process ChatbotService forwards to) running? `curl -i http://localhost:8100/health` — start it with `uvicorn main:app --reload --port 8100` from `src/AgentService`.
3. In BFF mode, is `GatewayBff` running with the current build (i.e. restarted since `ChatBffController` was added)? `curl -i http://localhost:5189/api/gateway/chat/health`
4. Does `ChatbotService`'s `ChatbotDb_Dev` database exist? `PythonAgentChatbotService.ProcessMessageAsync` writes chat history to it via EF Core; if `Program.cs`'s `EnsureCreatedAsync()` call failed silently (check the startup logs), messages will error out.

### The widget doesn't show up at all
This, unlike the chat itself, is a genuine frontend integration issue (script not loaded, CSS not applied) — the classic checks still apply here:
```javascript
console.log('Config loaded:', window.chatWidgetConfig);
console.log('Widget class available:', window.DistributedChatWidget);
fetch('/api/chatwidget/health').then(r => r.json()).then(console.log);
```

## ✅ Current State in Summary

- ✅ **The widget as a software artifact exists and is substantial** — it's not an empty scaffold.
- ✅ **Static file and configuration serving works** — script, demo, config, integration snippet.
- ✅ **BFF mode produces a real chat response**, via the new `ChatBffController` on GatewayBff forwarding to ChatbotService, which forwards to `AgentService` (Python/LangGraph). This is what ShopVerse (`CustomerWebsite`) uses.
- ✅ **Direct mode also works** (reaches `ChatController` → `PythonAgentChatbotService` → `AgentService` directly), but skips the BFF, so prefer BFF mode for anything inside `DistributedOrderSystem`.
- ❌ **The exposed statistics (`/api/chatwidget/stats`) are still random numbers**, not real telemetry — unchanged, not in scope of this fix.
- The Angular-based `src/` rewrite under `wwwroot/chat-widget/` (never finished, never wired to the served bundle) was deleted — see "Files Genuinely Present" above.
- For the NLP engine, authentication, and fine-tuning behind the scenes, see `CHATBOT_SERVICE_DOCUMENTATION.md` — that document may still describe `IChatbotService` as unregistered; it no longer is (see `PythonAgentChatbotService.cs`), so treat claims there about the chat endpoint being non-functional as outdated too, pending that document's own update pass.

## What Was Done to Make the Widget Actually Functional

1. Implemented `IChatbotService` via `PythonAgentChatbotService`, bridging to a Python `AgentService` (LangGraph + Anthropic) — done prior to this revision of the doc.
2. Added `ChatBffController` on GatewayBff, proxying `GET health` / `POST message` to ChatbotService, plus the matching `ServiceUrls:ChatbotService` config entry and named `HttpClient` registration.
3. Fixed the widget's `sendToBackend()` to POST to `.../message` instead of the never-implemented `.../analyze`.
4. Embedded the widget in ShopVerse's `_Layout.cshtml`, configured for BFF mode, plus fixed two unrelated stale-port bugs in `CustomerWebsite/appsettings.json` (`GatewayBff` and `ChatbotService` base URLs pointed at ports nothing was listening on).
5. Deleted the abandoned Angular widget scaffold (`src/`, `angular.json`) and corrected two stale example paths (`/api/chatbot/widget/` → `/api/chatwidget/`) in `demo.html`.
