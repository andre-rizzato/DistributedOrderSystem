# 🤖 ChatbotService - Chat Widget

> **Updated to reflect the real code.** The previous version of this document (v2.0.0, "December 16, 2025") described a production AI system with Microsoft DialoGPT-small via ONNX Runtime, measured NVIDIA GPU acceleration, a complete fine-tuning framework with real metrics (89.4% accuracy, 3.2GB GPU memory, etc.), and SQL Server as the database. **None of these claims match the code in the repository.** For a detailed breakdown of what's real and what's simulated in the NLP engine, authentication, and fine-tuning, see `CHATBOT_SERVICE_DOCUMENTATION.md` — this document focuses only on the **chat widget**, which is the most genuinely implemented part of the service, and on its limitations when it tries to talk to the backend.

## 🌟 Overview

The **Chat Widget** is a standalone JavaScript script, served by `ChatbotService`, meant to be embedded in any web page. Unlike the NLP engine and fine-tuning (both simulated, see `CHATBOT_SERVICE_DOCUMENTATION.md`), the widget **genuinely exists as an artifact**: TypeScript source, a minified build, an HTML demo, and a README are all present and substantial, not placeholders.

### Files Genuinely Present (verified)
```
src/ChatbotService/wwwroot/chat-widget/
├── angular.json                    # 19 lines — minimal build configuration
├── package.json                    # 35 lines
├── demo.html                       # 540 lines, ~20 KB — real interactive demo page
├── README.md                       # 405 lines, ~11.7 KB
├── dist/
│   └── chat-widget.min.js          # 824 lines, ~24 KB — bundle served by ChatWidgetController
└── src/
    ├── chat-widget.component.ts    # 864 lines, ~23 KB — widget source
    └── index.ts                    # 112 lines
```
These aren't skeleton files: `chat-widget.component.ts` genuinely implements theming, positioning, client-side session handling, etc. The problem isn't that the widget is fake — it's that **the backend it tries to talk to by default doesn't respond correctly** (see below).

## 🏗️ Real Architecture

```
Chat Widget (JS, wwwroot/chat-widget/dist/chat-widget.min.js)
    │
    │  fetch to baseUrl = useBffRouting ? config.bffBaseUrl : config.chatbotServiceUrl
    │  (default in source: useBffRouting: true, chatbotServiceUrl: '/api/chat')
    ▼
┌───────────────────────────────┬──────────────────────────────────────┐
│ BFF mode (default)            │ Direct mode                           │
│ calls {bffBaseUrl}/...        │ calls {chatbotServiceUrl}/...         │
│ default bffBaseUrl:           │ default: /api/chat                    │
│   /api/gateway/chat           │                                       │
│ ⚠️ GatewayBff exposes NO      │ ⚠️ Reaches ChatController, which       │
│ "gateway/chat" route           │ depends on IChatbotService — NEVER    │
│ — 404 either way               │ registered in DI → runtime error      │
└───────────────────────────────┴──────────────────────────────────────┘
```

**In practice, neither routing mode produces a real chat response today.** The widget loads, opens, and accepts input — but sending a message fails server-side in both configurations. This is independent of the NLP engine problems described in `CHATBOT_SERVICE_DOCUMENTATION.md`: even if `IChatbotService` were implemented and registered, BFF mode would still be broken until GatewayBff exposed a `/api/gateway/chat` route.

The widget's **static-serving** routes (script, demo, config, integration snippet) work correctly and are independent of this problem — see below.

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

**BFF mode** (effectively not working today, as explained above):
```html
<script src="/api/chatwidget/chat-widget.min.js"></script>
<script>
  window.chatWidgetConfig = {
    useBffRouting: true,
    bffBaseUrl: '/api/gateway/chat',  // ⚠️ this route doesn't exist on GatewayBff
    theme: 'light'
  };
</script>
```

**Direct mode** (reaches the right service, but the chat response still fails due to the DI problem described in `CHATBOT_SERVICE_DOCUMENTATION.md`):
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

## 📚 API Reference (verified routes)

### `GET /api/chatwidget/chat-widget.min.js`
Responds `application/javascript` with the real bundle content.

### `GET /api/chatwidget/config`
Supported query string: `theme`, `primaryColor`, `position`, `useBffRouting` (among others — see `WidgetConfigRequest` in the controller for the full list). Response: a JS script that sets `window.chatWidgetConfig`.

### `GET /api/chatwidget/demo`
Serves the real demo page — useful for visually checking the widget, keeping in mind that sending a message won't get a valid response from the backend in the current state.

## 🔍 Troubleshooting

### The widget doesn't get a response when I send a message
This is expected behavior today, not a misconfiguration on your part:
1. In BFF mode, `GatewayBff` has no route for chat at all — verify with `curl -i http://localhost:5189/api/gateway/chat` (expected: 404).
2. In Direct mode, the request reaches `ChatController` in ChatbotService, which fails because `IChatbotService` isn't registered in `Program.cs` — verify with `curl -i -X POST http://localhost:5055/api/chat/message -H "Content-Type: application/json" -d '{"message":"ciao"}'` (`"ciao"` — the Italian greeting the intent matcher checks for, see `CHATBOT_SERVICE_DOCUMENTATION.md`) and observe the returned error.

Before spending time debugging network/CORS issues, confirm which of the two cases above applies: both are known causes, not intermittent bugs.

### The widget doesn't show up at all
This, unlike the chat itself, is a genuine frontend integration issue (script not loaded, CSS not applied) — the classic checks still apply here:
```javascript
console.log('Config loaded:', window.chatWidgetConfig);
console.log('Widget class available:', window.DistributedChatWidget);
fetch('/api/chatwidget/health').then(r => r.json()).then(console.log);
```

## 🎯 Current State in Summary

- ✅ **The widget as a software artifact exists and is substantial** — it's not an empty scaffold.
- ✅ **Static file and configuration serving works** — script, demo, config, integration snippet.
- ❌ **No routing mode (BFF or Direct) produces a real chat response today** — for two independent reasons: GatewayBff doesn't expose a chat route, and ChatbotService's `ChatController` doesn't work due to a DI problem.
- ❌ **The exposed statistics (`/api/chatwidget/stats`) are random numbers**, not real telemetry.
- For the NLP engine, authentication, and fine-tuning behind the scenes, see `CHATBOT_SERVICE_DOCUMENTATION.md` — they're all simulated in a manner similar to what's described here for routing.

## Next Steps to Make the Widget Actually Functional

1. Implement `IChatbotService` and register it (see `CHATBOT_SERVICE_DOCUMENTATION.md`) — a prerequisite for either mode.
2. For BFF mode: add a proxy route on GatewayBff to `ChatbotService` (today there is no chat controller at all on GatewayBff).
3. Decide whether `useBffRouting: true` should stay the default, given it requires work not yet done on the GatewayBff side — in the meantime, Direct mode is the closest to working (it only needs point 1).
