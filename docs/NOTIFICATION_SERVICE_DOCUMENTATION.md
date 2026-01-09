# 📧 NotificationService - Sistema di Notifiche Multi-Canale

## 🎯 Panoramica

Il **NotificationService** è un microservizio completo per l'invio di notifiche attraverso diversi canali di comunicazione. Supporta SMS, Email, notifiche Push e notifiche in-app in tempo reale, fornendo un'interfaccia unificata per tutte le esigenze di notifica del sistema distribuito.

## ✨ Caratteristiche Principali

### 📱 **Multi-Canale**
- **SMS**: Integrazione con Twilio per invio internazionale
- **Email**: Supporto SMTP completo con allegati e HTML
- **Push**: Firebase Cloud Messaging per iOS/Android
- **In-App**: SignalR per notifiche real-time

### 🎨 **Sistema Template**
- Template parametrici con variabili `{nome}`
- Rendering automatico HTML e testo
- Validazione variabili richieste
- Template pre-configurati per casi comuni

### 🔄 **Background Processing**
- Job in background con Hangfire
- Notifiche programmate
- Retry automatici per fallimenti
- Dashboard di monitoraggio

### ⚡ **Performance e Scalabilità**
- Caching con Redis
- Invio batch per performance
- Connection pooling
- Health checks integrati

## 🏗️ Architettura

```
NotificationService/
├── Controllers/
│   └── NotificationChannelControllers.cs    # API REST per SMS/Email
├── Data/
│   └── NotificationContext.cs               # Entity Framework context
├── Models/
│   ├── NotificationModels.cs               # Entità database
│   ├── NotificationRequests.cs             # DTO richieste
│   ├── NotificationResponses.cs            # DTO risposte
│   └── NotificationSettings.cs             # Configurazioni
├── Services/
│   ├── INotificationServices.cs           # Interfacce
│   └── Implementations/
│       ├── SmsService.cs                  # Servizio SMS Twilio
│       ├── EmailService.cs                # Servizio Email MailKit
│       ├── PushService.cs                 # Servizio Push Firebase
│       ├── InAppNotificationService.cs    # Servizio Real-time SignalR
│       ├── NotificationTemplateService.cs  # Gestione template
│       └── NotificationService.cs          # Servizio principale
├── Configuration/
│   └── [Settings classes]                 # Classi configurazione
└── Program.cs                             # Bootstrap applicazione
```

## 📋 Provider Supportati

| Canale | Provider | Funzionalità | Ambiente |
|--------|----------|-------------|----------|
| **SMS** | Twilio | Invio internazionale, tracking stato | Produzione |
| **SMS** | Mock | Logging in console | Development |
| **Email** | MailKit/SMTP | HTML, allegati, CC/BCC | Produzione |
| **Email** | Mock | Logging in console | Development |
| **Push** | Firebase FCM | iOS/Android, topics, batch | Produzione |
| **Push** | Mock | Logging in console | Development |
| **Real-time** | SignalR + Redis | WebSockets, gruppi | Sempre attivo |

## 🔧 Configurazione

### **appsettings.json**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=DistributedOrderSystemNotifications;Trusted_Connection=true;MultipleActiveResultSets=true;",
    "Redis": "localhost:6379"
  },
  "Sms": {
    "AccountSid": "your-twilio-account-sid",
    "AuthToken": "your-twilio-auth-token",
    "FromPhoneNumber": "+1234567890",
    "MaxRetries": 3,
    "TimeoutSeconds": 30
  },
  "Email": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "UseSsl": false,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "FromEmail": "noreply@yourcompany.com",
    "FromName": "Your Company Name",
    "MaxRetries": 3,
    "TimeoutSeconds": 60
  },
  "PushNotification": {
    "ServiceAccountJson": "{\"type\":\"service_account\",\"project_id\":\"your-project\",\"private_key_id\":\"key-id\",\"private_key\":\"-----BEGIN PRIVATE KEY-----\\n...\\n-----END PRIVATE KEY-----\\n\",\"client_email\":\"firebase-service@your-project.iam.gserviceaccount.com\",\"client_id\":\"123456789\",\"auth_uri\":\"https://accounts.google.com/o/oauth2/auth\",\"token_uri\":\"https://oauth2.googleapis.com/token\"}",
    "DefaultAndroidIcon": "ic_notification",
    "DefaultAndroidColor": "#FF5722",
    "MaxRetries": 3
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "Database": 0,
    "KeyPrefix": "notifications:",
    "DefaultExpiry": "01:00:00"
  },
  "Templates": {
    "EnableCaching": true,
    "CacheExpiryMinutes": 60,
    "MaxTemplateSize": 1048576
  },
  "RateLimit": {
    "SmsPerMinute": 100,
    "EmailPerMinute": 50,
    "PushPerMinute": 1000,
    "BurstLimit": 200
  }
}
```

## 📬 API Endpoints

### **SMS Controller**

```http
# Invio SMS singolo
POST /api/sms/send
Content-Type: application/json
{
    "phoneNumber": "+393123456789",
    "message": "Ciao! Il tuo ordine #12345 è stato confermato.",
    "source": "OrderService",
    "metadata": {"orderId": "12345"}
}

# Invio SMS multipli
POST /api/sms/send-bulk
Content-Type: application/json
[
    {
        "phoneNumber": "+393123456789",
        "message": "Messaggio per primo utente"
    },
    {
        "phoneNumber": "+393987654321", 
        "message": "Messaggio per secondo utente"
    }
]

# Verifica stato SMS
GET /api/sms/status/{messageId}

# Validazione numero telefono
GET /api/sms/validate?phoneNumber=%2B393123456789
```

### **Email Controller**

```http
# Invio email singola
POST /api/email/send
Content-Type: application/json
{
    "to": "cliente@example.com",
    "subject": "Conferma ordine #12345",
    "content": "Grazie per il tuo ordine!",
    "htmlContent": "<h2>Grazie per il tuo ordine!</h2><p>Il tuo ordine #12345 è stato confermato.</p>",
    "cc": ["manager@example.com"],
    "metadata": {"orderId": "12345"}
}

# Invio con allegati
POST /api/email/send-with-attachments
Content-Type: application/json
{
    "emailRequest": {
        "to": "cliente@example.com",
        "subject": "Fattura ordine #12345",
        "content": "In allegato la fattura."
    },
    "attachments": [
        {
            "fileName": "fattura_12345.pdf",
            "contentBase64": "JVBERi0xLjQK...",
            "contentType": "application/pdf"
        }
    ]
}
```

### **Notifiche con Template**

```http
# Invio con template
POST /api/notifications/send-template
Content-Type: application/json
{
    "templateName": "order_confirmation",
    "recipient": "cliente@example.com",
    "type": "Email",
    "variables": {
        "customerName": "Mario Rossi",
        "orderId": "12345",
        "orderDetails": "2x Pizza Margherita",
        "total": "€25,00",
        "companyName": "Pizzeria Da Mario"
    }
}
```

## 🔄 SignalR Hub per Notifiche Real-time

### **Connessione Client (JavaScript)**

```javascript
// Connessione al hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/notifications")
    .withAutomaticReconnect()
    .build();

// Avvio connessione
await connection.start();

// Associazione a utente specifico
await connection.invoke("JoinUserGroup", "user123");

// Ascolta notifiche
connection.on("ReceiveNotification", (notification) => {
    console.log("Nuova notifica:", notification);
    showNotificationPopup(notification);
});

// Ascolta aggiornamenti contatore
connection.on("UnreadCountUpdate", (data) => {
    updateNotificationBadge(data.count);
});

// Gestione broadcast di sistema
connection.on("ReceiveBroadcast", (broadcast) => {
    showSystemNotification(broadcast);
});
```

### **Invio da Backend**

```csharp
// Inject del servizio
private readonly IInAppNotificationService _inAppService;

// Invio notifica a utente specifico
await _inAppService.SendInAppNotificationAsync(new SendInAppNotificationRequest
{
    UserId = "user123",
    Title = "Nuovo ordine ricevuto",
    Message = "Hai ricevuto un nuovo ordine #12345",
    Type = "order",
    Priority = NotificationPriority.High,
    Data = new Dictionary<string, object>
    {
        ["orderId"] = "12345",
        ["amount"] = 25.00m,
        ["redirectUrl"] = "/orders/12345"
    }
});

// Broadcast a tutti gli utenti connessi
await _inAppService.SendBroadcastNotificationAsync(
    "Manutenzione programmata",
    "Il sistema sarà in manutenzione dalle 02:00 alle 04:00",
    "warning"
);
```

## 🎨 Template System

### **Template Predefiniti**

Il sistema include 5 template predefiniti:

1. **order_confirmation** (Email) - Conferma ordine
2. **order_shipped** (SMS) - Notifica spedizione  
3. **payment_reminder** (Email) - Promemoria pagamento
4. **welcome_user** (Push) - Benvenuto nuovo utente
5. **system_maintenance** (InApp) - Manutenzione sistema

### **Creazione Template Personalizzati**

```http
POST /api/templates
Content-Type: application/json
{
    "name": "custom_promotion",
    "description": "Promozione personalizzata",
    "type": "Email",
    "subjectTemplate": "🎉 Offerta speciale per {customerName}!",
    "contentTemplate": "Ciao {customerName}!\n\nAbbiamo una promozione speciale per te: {promotionDetails}\n\nSconto: {discountPercentage}\nValida fino al: {expiryDate}\n\nNon perdere questa occasione!\n\n{companyName}",
    "htmlTemplate": "<h2>🎉 Offerta speciale!</h2><p>Ciao <strong>{customerName}</strong>!</p><p>{promotionDetails}</p><div class='discount'>Sconto: <span class='highlight'>{discountPercentage}</span></div><p>Valida fino al: {expiryDate}</p>",
    "variables": "{\"customerName\": \"Nome del cliente\", \"promotionDetails\": \"Dettagli promozione\", \"discountPercentage\": \"Percentuale sconto\", \"expiryDate\": \"Data scadenza\", \"companyName\": \"Nome azienda\"}",
    "isActive": true
}
```

## 🔍 Monitoring e Diagnostica

### **Health Checks**

```http
GET /health
```

Risposta:
```json
{
    "status": "Healthy",
    "checks": {
        "database": "Healthy",
        "redis": "Healthy", 
        "sms_provider": "Healthy",
        "email_provider": "Healthy"
    }
}
```

### **Hangfire Dashboard**

Accesso: `http://localhost:5000/hangfire`

- Monitoraggio job in background
- Statistiche invii
- Retry fallimenti automatici
- Programmazione notifiche future

### **Statistiche Notifiche**

```http
GET /api/notifications/stats?userId=user123&fromDate=2025-12-01&toDate=2025-12-16
```

## 🚀 Deployment

### **Docker Support**

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY . .
EXPOSE 80 443
ENTRYPOINT ["dotnet", "NotificationService.dll"]
```

### **Environment Variables**

```bash
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection="Server=sql-server;Database=Notifications;User Id=sa;Password=YourPassword;"
ConnectionStrings__Redis="redis-server:6379"
Sms__AccountSid="your-twilio-sid"
Sms__AuthToken="your-twilio-token"
```

## 🔐 Sicurezza

- ✅ Validazione input completa
- ✅ Rate limiting per prevenire spam
- ✅ Sanitizzazione dati sensibili nei log
- ✅ Cifratura credenziali provider
- ✅ CORS configurabile
- ✅ Health checks per monitoring

## 📊 Performance

- ✅ Connection pooling per SMTP
- ✅ Batch sending per notifiche multiple
- ✅ Caching template con Redis
- ✅ Retry policy configurabili
- ✅ Timeout configurabili per provider
- ✅ Background processing asincrono

## 🧪 Testing

### **Mock Services**

In ambiente `Development`, il servizio utilizza automaticamente implementazioni mock che:
- Loggano le notifiche in console invece di inviarle
- Permettono testing senza credenziali provider reali
- Simulano successi/fallimenti per testing

### **Test di Integrazione**

```bash
# Build del servizio
dotnet build

# Esecuzione test
dotnet test

# Avvio servizio
dotnet run --project NotificationService
```

## 💡 Esempi di Utilizzo

### **Scenario: Conferma Ordine**

```csharp
// 1. SMS di conferma immediata
await smsService.SendSmsAsync(new SendSmsRequest
{
    PhoneNumber = order.CustomerPhone,
    Message = $"Ordine #{order.Id} confermato! Totale: €{order.Total:F2}"
});

// 2. Email dettagliata
await notificationService.SendNotificationAsync(new SendTemplateNotificationRequest
{
    TemplateName = "order_confirmation",
    Recipient = order.CustomerEmail,
    Type = NotificationType.Email,
    Variables = new Dictionary<string, string>
    {
        ["customerName"] = order.CustomerName,
        ["orderId"] = order.Id.ToString(),
        ["orderDetails"] = order.GetDetailsString(),
        ["total"] = $"€{order.Total:F2}",
        ["companyName"] = "La Tua Pizzeria"
    }
});

// 3. Notifica in-app per admin
await inAppService.SendInAppNotificationAsync(new SendInAppNotificationRequest
{
    UserId = "admin",
    Title = "Nuovo ordine ricevuto",
    Message = $"Ordine #{order.Id} da {order.CustomerName}",
    Type = "new_order",
    Data = new Dictionary<string, object> { ["orderId"] = order.Id }
});
```

### **Scenario: Notifica Spedizione**

```csharp
// SMS con tracking
await notificationService.SendNotificationAsync(new SendTemplateNotificationRequest
{
    TemplateName = "order_shipped",
    Recipient = order.CustomerPhone,
    Type = NotificationType.SMS,
    Variables = new Dictionary<string, string>
    {
        ["customerName"] = order.CustomerName,
        ["orderId"] = order.Id.ToString(),
        ["trackingNumber"] = shipping.TrackingNumber,
        ["deliveryDate"] = shipping.EstimatedDelivery.ToString("dd/MM/yyyy")
    }
});

// Push notification
await pushService.SendToUserAsync(
    order.CustomerId,
    "Ordine spedito! 📦",
    $"Il tuo ordine #{order.Id} è in viaggio",
    new Dictionary<string, string>
    {
        ["action"] = "track_order",
        ["orderId"] = order.Id.ToString(),
        ["trackingUrl"] = shipping.TrackingUrl
    }
);
```

---

Il **NotificationService** fornisce una soluzione completa e scalabile per tutte le esigenze di notifica del sistema, garantendo affidabilità, performance e facilità d'uso.