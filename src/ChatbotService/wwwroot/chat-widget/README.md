# 🎨 Chat Widget - Sistema Distribuito Ordini

Il **Chat Widget** è una libreria JavaScript standalone che permette di integrare facilmente un'interfaccia di chat alimentata da AI in qualsiasi sito web.

## ✨ Caratteristiche Principali

### 🔄 Dual Routing Architecture
- **BFF Routing**: Integrazione ottimizzata per DistributedOrderSystem tramite GatewayBff
- **Direct Service**: Comunicazione diretta con ChatbotService per progetti esterni

### 🎨 Personalizzazione Completa
- **Temi**: Light, Dark, Auto (basato su preferenze sistema)
- **Colori**: Personalizzazione completa di colori primari e secondari
- **Posizione**: 4 posizioni angolari configurabili
- **Comportamento**: Auto-apertura, notifiche audio, indicatori di typing

### 📱 Responsive Design
- Layout ottimizzato per desktop e mobile
- Espansione automatica a schermo intero su dispositivi mobili
- Interfaccia touch-friendly

### 🤖 AI Integration
- Integrato con Microsoft DialoGPT-small (117M parametri)
- Supporto GPU per prestazioni ottimizzate
- Classificazione intenti e analisi sentiment
- Quick actions dinamiche basate sul contesto

## 🚀 Quick Start

### Modalità 1: BFF Routing (DistributedOrderSystem)

```html
<!DOCTYPE html>
<html>
<head>
    <title>Il Mio Sito</title>
</head>
<body>
    <h1>Benvenuto nel mio sito</h1>
    
    <!-- Chat Widget -->
    <script src="/api/chatwidget/chat-widget.min.js"></script>
    <script>
        window.chatWidgetConfig = {
            useBffRouting: true,
            bffBaseUrl: '/api/gateway/chat',
            theme: 'light',
            primaryColor: '#4299e1',
            position: 'bottom-right',
            botName: 'Assistente Vendite',
            welcomeMessage: 'Ciao! Posso aiutarti con il tuo ordine?'
        };
    </script>
</body>
</html>
```

### Modalità 2: Direct Service (Progetti Esterni)

```html
<!DOCTYPE html>
<html>
<head>
    <title>Progetto Esterno</title>
</head>
<body>
    <h1>Il mio e-commerce</h1>
    
    <!-- Chat Widget -->
    <script src="https://chatbot-service.example.com/api/chatwidget/chat-widget.min.js"></script>
    <script>
        window.chatWidgetConfig = {
            useBffRouting: false,
            chatbotServiceUrl: 'https://chatbot-service.example.com/api/chat',
            theme: 'dark',
            primaryColor: '#9f7aea',
            position: 'bottom-left',
            autoOpen: true,
            enableSoundNotifications: true
        };
    </script>
</body>
</html>
```

## ⚙️ Configurazione Avanzata

### Opzioni di Configurazione Complete

```javascript
window.chatWidgetConfig = {
    // Routing Configuration
    useBffRouting: true,                    // true = BFF routing, false = direct service
    bffBaseUrl: '/api/gateway/chat',        // URL base per BFF (se useBffRouting = true)
    chatbotServiceUrl: 'https://...',      // URL diretto ChatbotService (se useBffRouting = false)
    
    // Appearance
    theme: 'light',                         // 'light' | 'dark' | 'auto'
    primaryColor: '#4299e1',                // Colore principale (hex)
    secondaryColor: '#f7fafc',              // Colore secondario (hex)
    borderRadius: '12px',                   // Border radius (CSS value)
    
    // Position & Behavior
    position: 'bottom-right',               // 'bottom-right' | 'bottom-left' | 'top-right' | 'top-left'
    autoOpen: false,                        // Apri automaticamente al caricamento pagina
    showTypingIndicator: true,              // Mostra indicatore "sta scrivendo..."
    enableSoundNotifications: false,        // Abilita notifiche sonore
    maxMessages: 100,                       // Numero massimo messaggi in memoria
    
    // Bot Configuration
    botName: 'Assistente AI',               // Nome del bot
    botAvatar: 'https://...',               // URL avatar bot (opzionale)
    welcomeMessage: 'Ciao! Come posso aiutarti?',
    placeholderText: 'Scrivi un messaggio...',
    
    // Authentication (opzionale)
    jwtToken: 'your-auth-token',            // Token JWT per autenticazione
    sessionId: 'user-session-id'            // ID sessione personalizzato
};
```

### Controllo Programmatico

```javascript
// Inizializzazione manuale
const widget = new DistributedChatWidget({
    useBffRouting: true,
    theme: 'light'
});

// Controlli runtime
widget.show();                             // Mostra chat
widget.hide();                             // Nascondi chat
widget.toggle();                           // Toggle visibilità

// Aggiornamento configurazione
widget.updateConfig({
    theme: 'dark',
    primaryColor: '#9f7aea'
});

// Invio messaggi programmatici
widget.sendMessage('Ciao dal codice!');

// Aggiunta messaggi bot
widget.addBotMessage('Messaggio del bot');

// Cleanup
widget.destroy();                          // Rimuovi widget completamente
```

## 🛠️ Endpoint API

### Widget Resources
- `GET /api/chatwidget/chat-widget.min.js` - File JavaScript del widget
- `GET /api/chatwidget/demo` - Pagina demo interattiva
- `GET /api/chatwidget/config` - Configurazione dinamica
- `GET /api/chatwidget/health` - Status check

### Configuration API
```bash
# Configurazione dinamica con parametri
GET /api/chatwidget/config?theme=dark&primaryColor=%23ff6b6b&position=bottom-left

# Generazione snippet di integrazione
POST /api/chatwidget/integration-snippet
Content-Type: application/json

{
  "useBffRouting": true,
  "theme": "dark",
  "primaryColor": "#9f7aea",
  "position": "bottom-right",
  "autoOpen": false,
  "botName": "Assistente Personalizzato"
}
```

## 🎨 Personalizzazione CSS

### Override Stili Personalizzati

```css
/* Personalizzazione avanzata del widget */
.distributed-chat-widget {
    /* Personalizza il trigger button */
}

.distributed-chat-widget .chat-trigger {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%) !important;
    box-shadow: 0 8px 25px rgba(102, 126, 234, 0.4) !important;
}

.distributed-chat-widget .chat-window.theme-custom {
    background: #1a1a2e;
    border: 2px solid #16213e;
}

.distributed-chat-widget .chat-header {
    background: linear-gradient(135deg, #0f3460 0%, #e94560 100%) !important;
}

.distributed-chat-widget .message-bubble.user-message {
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%) !important;
}
```

## 📊 Monitoraggio e Analytics

### Health Check
```bash
GET /api/chatwidget/health
```

Response:
```json
{
  "status": "healthy",
  "service": "ChatWidget",
  "version": "1.0.0",
  "timestamp": "2025-01-21T10:30:00Z",
  "capabilities": [
    "dual-routing",
    "real-time-chat",
    "ai-integration",
    "customizable-themes",
    "responsive-design",
    "multi-language"
  ]
}
```

### Statistics (Admin Only)
```bash
GET /api/chatwidget/stats
Authorization: Bearer <admin-token>
```

## 🔧 Sviluppo e Debug

### Test Locale
1. Avvia ChatbotService: `dotnet run --project src/ChatbotService`
2. Apri demo: `http://localhost:5055/api/chatwidget/demo`
3. Testa configurazioni nella dashboard demo

### Demo Interattiva
La pagina demo include:
- ⚙️ **Pannello Configurazione**: Modifica opzioni in tempo reale
- 👁️ **Anteprima Live**: Vedi cambiamenti istantaneamente  
- 📝 **Codice Generato**: Snippet di integrazione automatico
- 🧪 **Test Tools**: Funzioni di testing integrate

### Build e Deployment

```bash
# Build ChatbotService
dotnet build src/ChatbotService

# Run in development
dotnet run --project src/ChatbotService

# Watch mode per sviluppo
dotnet watch --project src/ChatbotService
```

## 🌐 Integrazione Multi-Progetto

### Scenario 1: DistributedOrderSystem
- **Routing**: Tramite GatewayBff
- **Autenticazione**: JWT condiviso tra microservizi
- **Contesto**: Informazioni utente e ordini disponibili
- **Endpoint**: `/api/gateway/chat/*`

### Scenario 2: E-commerce Esterno  
- **Routing**: Diretto a ChatbotService
- **Autenticazione**: Token dedicato o anonimo
- **Contesto**: Configurabile tramite parametri
- **Endpoint**: `https://chatbot-service.com/api/chat/*`

### Scenario 3: Sito Aziendale
- **Routing**: CDN o hosting statico
- **Autenticazione**: Opzionale
- **Contesto**: Supporto generale
- **Endpoint**: Cross-origin configurabile

## 🚦 Troubleshooting

### Problemi Comuni

**Widget non appare:**
```javascript
// Verifica configurazione
console.log('Config:', window.chatWidgetConfig);
console.log('Widget class:', window.DistributedChatWidget);
```

**Errori CORS:**
```javascript
// Controlla se il servizio è raggiungibile
fetch('/api/chatwidget/health')
  .then(r => r.json())
  .then(data => console.log('Service status:', data))
  .catch(err => console.error('CORS or network error:', err));
```

**Stili non applicati:**
```javascript
// Verifica se i CSS sono stati inseriti
const styles = document.getElementById('chat-widget-styles');
console.log('Styles loaded:', !!styles);
```

### Debug Mode
```javascript
// Abilita logging dettagliato
window.chatWidgetConfig = {
    ...config,
    debug: true,  // Abilita console logging
    verboseLogging: true  // Log dettagliato delle operazioni
};
```

## 📚 Esempi Avanzati

### E-commerce con Product Context
```javascript
const widget = new DistributedChatWidget({
    useBffRouting: false,
    chatbotServiceUrl: 'https://api.example.com/chat',
    theme: 'light',
    contextData: {
        currentPage: 'product',
        productId: '12345',
        category: 'electronics',
        userType: 'premium'
    }
});

// Passa informazioni contestuali
widget.updateContext({
    cartItems: 3,
    lastPurchase: '2025-01-15'
});
```

### Multi-Language Support
```javascript
const widget = new DistributedChatWidget({
    useBffRouting: true,
    language: 'it',  // Supporto multilingua
    botName: 'Assistente Virtuale',
    welcomeMessage: 'Buongiorno! Come posso aiutarla oggi?',
    placeholderText: 'Digita qui il tuo messaggio...',
    quickActions: [
        'Informazioni prodotto',
        'Stato ordine', 
        'Supporto tecnico',
        'Parla con operatore'
    ]
});
```

## 🔐 Sicurezza

### Best Practices
- ✅ Sempre utilizzare HTTPS in produzione
- ✅ Validare token JWT lato server
- ✅ Implementare rate limiting
- ✅ Sanitizzare input utente
- ✅ Configurare CSP headers appropriati

### Content Security Policy
```html
<meta http-equiv="Content-Security-Policy" content="
    default-src 'self';
    script-src 'self' 'unsafe-inline';
    connect-src 'self' https://your-chatbot-service.com;
    img-src 'self' data: https:;
    style-src 'self' 'unsafe-inline';
">
```

## 🆕 Roadmap

### Prossime Funzionalità
- 🔊 **Voice Integration**: Supporto audio input/output
- 📎 **File Upload**: Caricamento documenti e immagini  
- 🎯 **Advanced Analytics**: Metriche dettagliate conversazioni
- 🌍 **I18n Complete**: Supporto multilingua esteso
- 🤖 **Chatbot Builder**: Editor visuale per flussi conversazionali
- 📱 **Mobile SDK**: Versioni native iOS/Android

---

## 💡 Supporto

Per domande, bug reports o richieste di funzionalità:

- 📧 **Email**: support@distributed-order-system.com
- 🐛 **Issues**: GitHub Issues
- 📖 **Docs**: [Documentation Portal](https://docs.distributed-order-system.com)
- 💬 **Community**: [Discord Server](https://discord.gg/distributed-orders)

---

*Sviluppato con ❤️ per il Sistema Distribuito Ordini - 2025*