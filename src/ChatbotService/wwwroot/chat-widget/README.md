# 🎨 Chat Widget - Distributed Order System

The **Chat Widget** is a standalone JavaScript library that makes it easy to integrate an AI-powered chat interface into any website.

## ✨ Main Features

### 🔄 Dual Routing Architecture
- **BFF Routing**: Optimized integration for DistributedOrderSystem via GatewayBff
- **Direct Service**: Direct communication with ChatbotService for external projects

### 🎨 Full Customization
- **Themes**: Light, Dark, Auto (based on system preferences)
- **Colors**: Full customization of primary and secondary colors
- **Position**: 4 configurable corner positions
- **Behavior**: Auto-open, sound notifications, typing indicators

### 📱 Responsive Design
- Layout optimized for desktop and mobile
- Automatic full-screen expansion on mobile devices
- Touch-friendly interface

### 🤖 AI Integration
- Integrated with Microsoft DialoGPT-small (117M parameters)
- GPU support for optimized performance
- Intent classification and sentiment analysis
- Dynamic quick actions based on context

## 🚀 Quick Start

### Mode 1: BFF Routing (DistributedOrderSystem)

```html
<!DOCTYPE html>
<html>
<head>
    <title>My Site</title>
</head>
<body>
    <h1>Welcome to my site</h1>

    <!-- Chat Widget -->
    <script src="/api/chatwidget/chat-widget.min.js"></script>
    <script>
        window.chatWidgetConfig = {
            useBffRouting: true,
            bffBaseUrl: '/api/gateway/chat',
            theme: 'light',
            primaryColor: '#4299e1',
            position: 'bottom-right',
            botName: 'Sales Assistant',
            welcomeMessage: 'Hi! Can I help you with your order?'
        };
    </script>
</body>
</html>
```

### Mode 2: Direct Service (External Projects)

```html
<!DOCTYPE html>
<html>
<head>
    <title>External Project</title>
</head>
<body>
    <h1>My e-commerce store</h1>

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

## ⚙️ Advanced Configuration

### Full Configuration Options

```javascript
window.chatWidgetConfig = {
    // Routing Configuration
    useBffRouting: true,                    // true = BFF routing, false = direct service
    bffBaseUrl: '/api/gateway/chat',        // Base URL for BFF (if useBffRouting = true)
    chatbotServiceUrl: 'https://...',      // Direct ChatbotService URL (if useBffRouting = false)

    // Appearance
    theme: 'light',                         // 'light' | 'dark' | 'auto'
    primaryColor: '#4299e1',                // Primary color (hex)
    secondaryColor: '#f7fafc',              // Secondary color (hex)
    borderRadius: '12px',                   // Border radius (CSS value)

    // Position & Behavior
    position: 'bottom-right',               // 'bottom-right' | 'bottom-left' | 'top-right' | 'top-left'
    autoOpen: false,                        // Open automatically on page load
    showTypingIndicator: true,              // Show "typing..." indicator
    enableSoundNotifications: false,        // Enable sound notifications
    maxMessages: 100,                       // Maximum number of messages kept in memory

    // Bot Configuration
    botName: 'AI Assistant',                // Bot name
    botAvatar: 'https://...',               // Bot avatar URL (optional)
    welcomeMessage: 'Hi! How can I help you?',
    placeholderText: 'Type a message...',

    // Authentication (optional)
    jwtToken: 'your-auth-token',            // JWT token for authentication
    sessionId: 'user-session-id'            // Custom session ID
};
```

### Programmatic Control

```javascript
// Manual initialization
const widget = new DistributedChatWidget({
    useBffRouting: true,
    theme: 'light'
});

// Runtime controls
widget.show();                             // Show chat
widget.hide();                             // Hide chat
widget.toggle();                           // Toggle visibility

// Update configuration
widget.updateConfig({
    theme: 'dark',
    primaryColor: '#9f7aea'
});

// Send messages programmatically
widget.sendMessage('Hi from the code!');

// Add bot messages
widget.addBotMessage('Bot message');

// Cleanup
widget.destroy();                          // Completely remove the widget
```

## 🛠️ API Endpoints

### Widget Resources
- `GET /api/chatwidget/chat-widget.min.js` - Widget JavaScript file
- `GET /api/chatwidget/demo` - Interactive demo page
- `GET /api/chatwidget/config` - Dynamic configuration
- `GET /api/chatwidget/health` - Status check

### Configuration API
```bash
# Dynamic configuration with parameters
GET /api/chatwidget/config?theme=dark&primaryColor=%23ff6b6b&position=bottom-left

# Generate an integration snippet
POST /api/chatwidget/integration-snippet
Content-Type: application/json

{
  "useBffRouting": true,
  "theme": "dark",
  "primaryColor": "#9f7aea",
  "position": "bottom-right",
  "autoOpen": false,
  "botName": "Custom Assistant"
}
```

## 🎨 CSS Customization

### Custom Style Overrides

```css
/* Advanced widget customization */
.distributed-chat-widget {
    /* Customize the trigger button */
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

## 📊 Monitoring and Analytics

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

## 🔧 Development and Debugging

### Local Testing
1. Start ChatbotService: `dotnet run --project src/ChatbotService`
2. Open the demo: `http://localhost:5055/api/chatwidget/demo`
3. Test configurations in the demo dashboard

### Interactive Demo
The demo page includes:
- ⚙️ **Configuration Panel**: Change options in real time
- 👁️ **Live Preview**: See changes instantly
- 📝 **Generated Code**: Automatic integration snippet
- 🧪 **Test Tools**: Built-in testing functions

### Build and Deployment

```bash
# Build ChatbotService
dotnet build src/ChatbotService

# Run in development
dotnet run --project src/ChatbotService

# Watch mode for development
dotnet watch --project src/ChatbotService
```

## 🌐 Multi-Project Integration

### Scenario 1: DistributedOrderSystem
- **Routing**: Via GatewayBff
- **Authentication**: JWT shared across microservices
- **Context**: User and order information available
- **Endpoint**: `/api/gateway/chat/*`

### Scenario 2: External E-commerce
- **Routing**: Direct to ChatbotService
- **Authentication**: Dedicated token or anonymous
- **Context**: Configurable via parameters
- **Endpoint**: `https://chatbot-service.com/api/chat/*`

### Scenario 3: Corporate Website
- **Routing**: CDN or static hosting
- **Authentication**: Optional
- **Context**: General support
- **Endpoint**: Configurable cross-origin

## 🚦 Troubleshooting

### Common Issues

**Widget doesn't appear:**
```javascript
// Check configuration
console.log('Config:', window.chatWidgetConfig);
console.log('Widget class:', window.DistributedChatWidget);
```

**CORS errors:**
```javascript
// Check whether the service is reachable
fetch('/api/chatwidget/health')
  .then(r => r.json())
  .then(data => console.log('Service status:', data))
  .catch(err => console.error('CORS or network error:', err));
```

**Styles not applied:**
```javascript
// Check whether the CSS was injected
const styles = document.getElementById('chat-widget-styles');
console.log('Styles loaded:', !!styles);
```

### Debug Mode
```javascript
// Enable verbose logging
window.chatWidgetConfig = {
    ...config,
    debug: true,  // Enable console logging
    verboseLogging: true  // Detailed operation logging
};
```

## 📚 Advanced Examples

### E-commerce with Product Context
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

// Pass contextual information
widget.updateContext({
    cartItems: 3,
    lastPurchase: '2025-01-15'
});
```

### Multi-Language Support
```javascript
const widget = new DistributedChatWidget({
    useBffRouting: true,
    language: 'en',  // Multi-language support
    botName: 'Virtual Assistant',
    welcomeMessage: 'Good morning! How can I help you today?',
    placeholderText: 'Type your message here...',
    quickActions: [
        'Product information',
        'Order status',
        'Technical support',
        'Talk to an agent'
    ]
});
```

## 🔐 Security

### Best Practices
- ✅ Always use HTTPS in production
- ✅ Validate JWT tokens server-side
- ✅ Implement rate limiting
- ✅ Sanitize user input
- ✅ Configure appropriate CSP headers

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

### Upcoming Features
- 🔊 **Voice Integration**: Audio input/output support
- 📎 **File Upload**: Document and image uploads
- 🎯 **Advanced Analytics**: Detailed conversation metrics
- 🌍 **I18n Complete**: Extended multi-language support
- 🤖 **Chatbot Builder**: Visual editor for conversation flows
- 📱 **Mobile SDK**: Native iOS/Android versions

---

## 💡 Support

For questions, bug reports, or feature requests:

- 📧 **Email**: support@distributed-order-system.com
- 🐛 **Issues**: GitHub Issues
- 📖 **Docs**: [Documentation Portal](https://docs.distributed-order-system.com)
- 💬 **Community**: [Discord Server](https://discord.gg/distributed-orders)

---

*Built with ❤️ for the Distributed Order System - 2025*
