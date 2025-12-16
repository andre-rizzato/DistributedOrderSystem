# 🤖 DOCUMENTAZIONE CHATBOT SERVICE

## Panoramica

Il **ChatbotService** è un microservizio intelligente che fornisce capacità di chatbot AI per il supporto clienti e le vendite. Utilizza modelli di machine learning per comprendere l'intento dell'utente e fornire risposte personalizzate.

## Caratteristiche Principali

### 🧠 Intelligenza Artificiale
- **Classificazione degli intenti**: Comprende cosa vuole l'utente
- **NLP leggero**: Elaborazione del linguaggio naturale efficiente
- **Fine-tuning personalizzato**: Addestramento del modello su dati specifici
- **Apprendimento continuo**: Miglioramento basato sulle interazioni

### 🔐 Sicurezza e Autenticazione
- **JWT (JSON Web Token)**: Autenticazione stateless e sicura
- **Autorizzazione basata sui ruoli**: Controllo granulare degli accessi
- **Policy di sicurezza**: Protezione degli endpoint critici
- **Validazione rigorosa**: Controlli di sicurezza multi-livello

### 🔗 Integrazione con Microservizi
- **OrderService**: Gestione ordini e stato delle commesse
- **ProductService**: Ricerca e informazioni sui prodotti
- **PaymentService**: Elaborazione pagamenti e transazioni
- **InventoryService**: Controllo disponibilità e stock

### 📊 Monitoring e Diagnostica
- **Health checks**: Monitoraggio dello stato del servizio
- **Logging strutturato**: Registrazione dettagliata degli eventi
- **Metriche personalizzate**: Analisi delle performance
- **Dashboard amministrativo**: Interfaccia di gestione

## Architettura Tecnica

### Stack Tecnologico
```
- .NET 9.0 (Framework principale)
- Entity Framework Core 9.0.0 (ORM Database)
- ML.NET 4.0.0 (Machine Learning)
- Redis (Cache e Sessioni)
- JWT Bearer Authentication
- Swagger/OpenAPI (Documentazione)
```

### Pattern Architetturali
- **Microservizi**: Servizio indipendente e scalabile
- **CQRS**: Separazione tra comandi e query
- **Repository Pattern**: Astrazione del livello dati
- **Dependency Injection**: Gestione delle dipendenze
- **Clean Architecture**: Separazione delle responsabilità

## Struttura del Progetto

```
ChatbotService/
├── 📁 Configuration/          # Configurazioni del sistema
│   ├── AuthSettings.cs        # Impostazioni JWT
│   ├── ModelSettings.cs       # Configurazione modelli AI
│   └── ServiceUrlsSettings.cs # URL microservizi
├── 📁 Controllers/            # Controller REST API
│   ├── ChatController.cs      # API per chat e conversazioni
│   └── SimpleAdminController.cs # Dashboard amministrativo
├── 📁 Data/                   # Layer di accesso ai dati
│   ├── ChatContext.cs         # Contesto Entity Framework
│   └── Entities/              # Entità del dominio
├── 📁 Models/                 # Modelli di trasferimento dati
│   ├── ChatModels.cs          # Modelli chat e messaggi
│   ├── IntentModels.cs        # Classificazione intenti
│   └── AuthModels.cs          # Autenticazione e autorizzazione
├── 📁 Services/               # Servizi di business logic
│   ├── Interfaces/            # Contratti dei servizi
│   ├── LightweightNLPService.cs    # NLP senza modelli pesanti
│   ├── AuthenticationService.cs    # Gestione autenticazione
│   └── ServiceIntegrationService.cs # Comunicazione microservizi
└── Program.cs                 # Entry point dell'applicazione
```

## Configurazione e Setup

### 1. Variabili di Ambiente

```json
{
  "ASPNETCORE_ENVIRONMENT": "Development",
  "ASPNETCORE_URLS": "http://localhost:5055"
}
```

### 2. Stringa di Connessione Database

```json
{
  "ConnectionStrings": {
    "ChatbotDb": "Server=localhost;Database=ChatbotDb;Trusted_Connection=true;TrustServerCertificate=true;",
    "Redis": "localhost:6379"
  }
}
```

### 3. Configurazioni JWT

```json
{
  "Auth": {
    "Secret": "your-super-secret-key-at-least-256-bits-long",
    "Issuer": "ChatbotService",
    "Audience": "DistributedOrderSystem",
    "ExpirationHours": 24
  }
}
```

### 4. Impostazioni del Modello AI

```json
{
  "ModelSettings": {
    "DefaultModelPath": "./Models/lightweight-nlp.onnx",
    "FineTunedModelPath": "./Models/custom-model.onnx",
    "MaxTokens": 150,
    "Temperature": 0.7,
    "UseGPU": false
  }
}
```

## API Endpoints

### 🤖 Chat API

#### POST /api/chat/message
Invia un messaggio al chatbot e riceve una risposta intelligente.

**Request Body:**
```json
{
  "message": "Voglio ordinare una pizza",
  "userId": "user123",
  "sessionId": "session456"
}
```

**Response:**
```json
{
  "response": "Perfetto! Ti aiuto a ordinare una pizza. Che tipo preferisci?",
  "intent": "order_food",
  "confidence": 0.95,
  "sessionId": "session456",
  "suggestedActions": ["view_menu", "customize_pizza"]
}
```

#### GET /api/chat/demo
Dashboard di demo per testare il chatbot con interfaccia semplice.

#### GET /api/chat/history/{sessionId}
Recupera la cronologia dei messaggi per una sessione specifica.

### 🔐 Authentication API

#### POST /api/auth/login
Autenticazione utente con credenziali.

**Request Body:**
```json
{
  "username": "admin",
  "password": "password123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2025-01-01T00:00:00Z",
  "role": "Admin"
}
```

#### POST /api/auth/register
Registrazione nuovo utente.

### 📊 Admin API

#### GET /api/admin
Dashboard amministrativo con statistiche e metriche.

#### GET /api/admin/stats
Statistiche dettagliate sull'utilizzo del chatbot.

#### POST /api/admin/retrain
Avvia il processo di fine-tuning del modello.

## Utilizzo in Sviluppo

### 1. Avvio con Visual Studio Code

```bash
# Metodo 1: Usando tasks.json
Ctrl+Shift+P -> Tasks: Run Task -> 🤖 run ChatbotService

# Metodo 2: Usando launch.json
F5 -> Seleziona "🤖 Debug ChatbotService"

# Metodo 3: Da terminale
cd src/ChatbotService
dotnet run
```

### 2. Modalità Watch (Auto-reload)

```bash
# Usando VS Code task
Ctrl+Shift+P -> Tasks: Run Task -> 🤖 watch ChatbotService

# Da terminale
cd src/ChatbotService
dotnet watch
```

### 3. Test delle API

Dopo l'avvio, visita:
- **Swagger UI**: http://localhost:5055/swagger
- **Chat Demo**: http://localhost:5055/api/chat/demo  
- **Admin Dashboard**: http://localhost:5055/api/admin
- **Health Check**: http://localhost:5055/health

## Integrazione con Altri Servizi

### Chiamate ai Microservizi

Il ChatbotService può comunicare con altri servizi tramite HTTP:

```csharp
// Esempio: Ottenere informazioni su un prodotto
var product = await _serviceIntegration.GetProductAsync(productId);

// Esempio: Creare un ordine
var order = await _serviceIntegration.CreateOrderAsync(orderRequest);

// Esempio: Verificare disponibilità
var availability = await _serviceIntegration.CheckInventoryAsync(productId);
```

### Pattern di Comunicazione

1. **Sincrona HTTP**: Per operazioni immediate
2. **Asincrona con Redis**: Per notifiche e cache
3. **Event-driven**: Per aggiornamenti di stato (futuro)

## Machine Learning e AI

### Classificazione degli Intenti

Il servizio NLP classifica i messaggi dell'utente in diversi intenti:

- `greeting` - Saluti e convenevoli
- `order_request` - Richieste di ordine
- `product_inquiry` - Domande sui prodotti
- `support_request` - Richieste di supporto
- `payment_issue` - Problemi di pagamento
- `complaint` - Reclami

### Fine-tuning del Modello

```csharp
// Processo di addestramento personalizzato
var fineTuningService = new FineTuningService();
await fineTuningService.TrainModelAsync(trainingData);
```

## Sicurezza e Best Practices

### 1. Autenticazione JWT

- Token con scadenza configurabile
- Chiavi di firma robuste
- Validazione rigorosa dei claim
- Refresh token per sessioni lunghe

### 2. Autorizzazione

- Policy basate sui ruoli
- Controllo granulare degli endpoint
- Validazione dei permessi per risorsa

### 3. Protezione dei Dati

- Crittografia delle informazioni sensibili
- Hashing sicuro delle password
- Sanitizzazione degli input utente

## Monitoraggio e Diagnostica

### Health Checks

Il servizio espone un endpoint di health check:

```json
{
  "Status": "Healthy",
  "Timestamp": "2025-01-15T10:30:00Z",
  "Version": "1.0.0",
  "Service": "ChatbotService"
}
```

### Logging

Il sistema utilizza logging strutturato:

```csharp
_logger.LogInformation("🤖 ChatbotService avviato con successo");
_logger.LogWarning("Modello fine-tuned non trovato: {Path}", modelPath);
_logger.LogError(ex, "Errore durante l'elaborazione del messaggio");
```

### Metriche

- Tempo di risposta medio
- Numero di messaggi processati
- Accuratezza della classificazione degli intenti
- Utilizzo risorse sistema

## Troubleshooting

### Problemi Comuni

1. **Errore di connessione al database**
   ```
   Soluzione: Verificare che SQL Server sia in esecuzione
   Comando: docker run -e "SA_PASSWORD=YourPassword123!" -p 1433:1433 -d mcr.microsoft.com/mssql/server
   ```

2. **Redis non disponibile**
   ```
   Soluzione: Avviare Redis localmente
   Comando: docker run -d -p 6379:6379 redis
   ```

3. **Token JWT scaduto**
   ```
   Soluzione: Fare login nuovamente o implementare refresh token
   ```

4. **Modello AI non trovato**
   ```
   Soluzione: Il servizio utilizza NLP leggero come fallback
   ```

### Debug e Diagnostica

```bash
# Visualizza logs dettagliati
dotnet run --verbosity diagnostic

# Controlla health checks
curl http://localhost:5055/health

# Test delle API con curl
curl -X POST http://localhost:5055/api/chat/message \
  -H "Content-Type: application/json" \
  -d '{"message": "Ciao!", "userId": "test"}'
```

## Roadmap Futura

### 🚀 Prossime Funzionalità

1. **Integrazione WhatsApp**: Connettore per WhatsApp Business API
2. **Modelli più avanzati**: Integrazione con GPT-4 o modelli locali
3. **Chat vocale**: Supporto per input/output vocale
4. **Analisi sentiment**: Analisi dell'umore del cliente
5. **Multi-lingua**: Supporto per più lingue
6. **Dashboard analytics**: Interfaccia avanzata per metriche

### 🔧 Miglioramenti Tecnici

1. **Caching intelligente**: Cache predittiva delle risposte
2. **Scalabilità orizzontale**: Supporto cluster Redis
3. **Event sourcing**: Tracciamento completo delle conversazioni
4. **GraphQL**: API più flessibili per il frontend
5. **gRPC**: Comunicazione ad alte performance tra servizi

---

## 📞 Supporto

Per domande o problemi:
- **Email**: support@distributedordersystem.com
- **Documentazione**: /swagger
- **Repository**: GitHub del progetto
- **Issue Tracking**: GitHub Issues

**Versione Documento**: 1.0.0
**Ultima Modifica**: Dicembre 2025
**Autore**: Team Distributed Order System