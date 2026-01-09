# ShopVerse Platform - Documentazione Completa

## 🚀 Panoramica del Progetto

**ShopVerse** è una piattaforma e-commerce moderna sviluppata come parte dell'architettura distribuita del `DistributedOrderSystem`. Rappresenta l'interfaccia frontend orientata al cliente, progettata per offrire un'esperienza shopping intuitiva e performante.

### 📊 Informazioni Generali

- **Nome Progetto**: ShopVerse CustomerWebsite
- **Versione**: 1.0.0
- **Framework**: ASP.NET Core 9.0 MVC
- **Architettura**: Microservizi distribuiti
- **Ambiente**: Development & Production Ready
- **Autore**: Rizzato Sistemas
- **Data Creazione**: 17 Dicembre 2025

---

## 🎯 Obiettivi Architetturali

### Obiettivi Primari
1. **Interfaccia Cliente Moderna**: UI/UX contemporanea per e-commerce
2. **Integrazione Microservizi**: Comunicazione fluida con backend distribuiti
3. **Performance Ottimizzate**: Caricamento rapido e esperienza reattiva
4. **Scalabilità Orizzontale**: Architettura pronta per crescita del traffico
5. **Manutenibilità**: Codice pulito e pattern consolidati

### Obiettivi Secondari
- **SEO Friendly**: Struttura ottimizzata per motori di ricerca
- **Accessibilità**: Conformità WCAG per inclusività
- **Mobile-First**: Priorità dispositivi mobili
- **Internazionalizzazione**: Pronto per localizzazione multilingua

---

## 🏗️ Architettura Tecnica

### Stack Tecnologico

| Componente | Tecnologia | Versione | Scopo |
|------------|------------|----------|-------|
| **Backend Framework** | ASP.NET Core MVC | 9.0 | Logica applicativa e routing |
| **Frontend Framework** | Bootstrap | 5.3 | Sistema di layout responsivo |
| **Stili** | CSS3 + Custom Properties | - | Design system personalizzato |
| **JavaScript** | ES6+ Vanilla | - | Interattività client-side |
| **Comunicazione HTTP** | HttpClient | .NET 9.0 | Integrazione microservizi |
| **Gestione Stato** | ASP.NET Session | 9.0 | Persistenza sessioni utente |
| **Logging** | Microsoft.Extensions.Logging | 9.0 | Monitoraggio e debugging |

### Pattern Architetturali Implementati

#### 1. **Model-View-Controller (MVC)**
- **Models**: DTOs per comunicazione API
- **Views**: Razor Pages per rendering HTML
- **Controllers**: Logica di coordinamento tra frontend e backend

#### 2. **Dependency Injection**
- **Servizi**: ProductService, OrderService, ShoppingCartService
- **Configurazione**: Injection container automatico
- **Testabilità**: Mocking facilitato per unit testing

#### 3. **Repository Pattern (via Services)**
- **Astrazione Dati**: Servizi incapsulano chiamate API
- **Separazione Responsabilità**: Business logic separata da data access
- **Resilienza**: Gestione errori centralizzata

---

## 📁 Struttura del Progetto

```
CustomerWebsite/
├── 📂 Controllers/               # Logica di controllo MVC
│   ├── 🎮 HomeController.cs        # Homepage e navigazione principale
│   ├── 🛒 CartController.cs        # Gestione carrello spesa
│   ├── 📦 ProductController.cs     # Catalogo e dettagli prodotti
│   └── 💳 CheckoutController.cs    # Processo di acquisto
│
├── 📂 Models/                    # Modelli di dati e DTOs
│   ├── 📦 ProductModels.cs         # DTOs per prodotti e categorie
│   ├── 🛒 ShoppingModels.cs        # DTOs per carrello e wishlist
│   ├── 📋 OrderModels.cs           # DTOs per ordini e checkout
│   └── 👤 AccountModels.cs         # DTOs per autenticazione utente
│
├── 📂 Services/                  # Livello di servizio per API
│   ├── 📦 ProductService.cs        # Integrazione ProductService API
│   ├── 🛒 ShoppingCartService.cs   # Integrazione CartService API
│   └── 📋 OrderService.cs          # Integrazione OrderService API
│
├── 📂 Views/                     # Template Razor per UI
│   ├── 📂 Home/
│   │   └── 🏠 Index.cshtml           # Homepage principale
│   └── 📂 Shared/
│       ├── 🎨 _Layout.cshtml         # Layout master comune
│       └── ❌ Error.cshtml           # Pagina errori
│
├── 📂 wwwroot/                   # Asset statici pubblici
│   ├── 📂 css/
│   │   ├── 🎨 shopverse.css          # Stili personalizzati ShopVerse
│   │   └── 🎨 site.css               # Stili base applicazione
│   ├── 📂 js/
│   │   ├── ⚡ shopverse.js           # Logica JavaScript interattiva
│   │   └── ⚡ site.js                # Utilità JavaScript base
│   └── 📂 lib/                    # Librerie esterne (Bootstrap, jQuery)
│
├── ⚙️ Program.cs                 # Configurazione applicazione
├── ⚙️ appsettings.json          # Configurazioni ambiente
└── 📖 README.md                 # Documentazione progetto
```

---

## 🎨 Design System - ShopVerse Branding

### Palette Colori

| Colore | Hex Code | RGB | Utilizzo |
|--------|----------|-----|----------|
| **Viola Primario** | `#7C3AED` | rgb(124, 58, 237) | Elementi principali, CTA |
| **Indaco Secondario** | `#4F46E5` | rgb(79, 70, 229) | Link, accenti |
| **Teal Terziario** | `#14B8A6` | rgb(20, 184, 166) | Successo, conferme |
| **Grigio Neutro** | `#6B7280` | rgb(107, 114, 128) | Testi secondari |
| **Bianco Puro** | `#FFFFFF` | rgb(255, 255, 255) | Sfondi, contrasti |

### Tipografia

- **Famiglia Font**: System UI, Segoe UI, Roboto, sans-serif
- **Dimensioni Base**: 16px (1rem)
- **Scala Tipografica**: Modular Scale 1.25 (Major Third)
- **Pesi Font**: 400 (Regular), 500 (Medium), 600 (Semibold), 700 (Bold)

### Componenti UI

#### 1. **Bottoni**
```css
.btn-shopverse {
    background: linear-gradient(135deg, #7C3AED, #4F46E5);
    border-radius: 50px;
    padding: 10px 25px;
    font-weight: 600;
    transition: all 0.3s ease;
}
```

#### 2. **Cards Prodotto**
```css
.product-card {
    border-radius: 15px;
    box-shadow: 0 8px 25px rgba(124, 58, 237, 0.1);
    transition: transform 0.3s ease;
}
```

#### 3. **Header Navigation**
```css
.shopverse-header {
    background: linear-gradient(135deg, #1e293b, #334155);
    backdrop-filter: blur(10px);
}
```

---

## 🔧 Funzionalità Implementate

### Core Features

#### 1. **Homepage Dinamica**
- **Hero Section**: Banner principale con call-to-action
- **Prodotti in Evidenza**: Showcase prodotti consigliati
- **Categorie**: Navigazione per categorie merceologiche
- **Bestseller**: Prodotti più venduti con rating
- **Search Bar**: Ricerca intelligente con autocomplete

#### 2. **Catalogo Prodotti**
- **Grid Responsiva**: Layout adattivo per diversi dispositivi
- **Filtri Avanzati**: Per categoria, prezzo, rating, brand
- **Ordinamento**: Per rilevanza, prezzo, popolarità, novità
- **Paginazione**: Caricamento progressivo risultati

#### 3. **Carrello Spesa**
- **Gestione Sessioni**: Persistenza carrello tra sessioni
- **Aggiornamento Real-time**: Sincronizzazione immediata
- **Calcolo Totali**: Subtotali, tasse, spedizione automatici
- **Quantità Dinamiche**: Modifiche quantità in tempo reale

#### 4. **Esperienza Utente**
- **Responsive Design**: Mobile-first approach
- **Performance Ottimizzate**: Lazy loading, caching
- **Accessibilità**: ARIA labels, navigazione keyboard
- **Feedback Visivo**: Loading states, animazioni fluide

### Advanced Features

#### 1. **Integrazione Microservizi**
```csharp
// ProductService Integration
public async Task<List<ProductDto>> GetFeaturedProductsAsync(int count = 8)
{
    var response = await _httpClient.GetAsync($"{_baseUrl}/api/products/featured?count={count}");
    return await response.Content.ReadFromJsonAsync<List<ProductDto>>();
}
```

#### 2. **Gestione Errori Resiliente**
```csharp
// Graceful Degradation Pattern
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "Errore durante il recupero dei prodotti");
    return new List<ProductDto>(); // Fallback vuoto
}
```

#### 3. **Session Management**
```csharp
// Carrello Persistente
services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
```

---

## 🌐 Integrazione Microservizi

### Endpoints API

| Servizio | Endpoint | Porta | Funzione |
|----------|----------|-------|----------|
| **ProductService** | `https://localhost:5003` | 5003 | Gestione catalogo prodotti |
| **OrderService** | `https://localhost:5007` | 5007 | Gestione ordini e carrello |
| **InventoryService** | `https://localhost:5005` | 5005 | Gestione stock e disponibilità |
| **NotificationService** | `https://localhost:5009` | 5009 | Notifiche email/SMS |

### Comunicazione HTTP

#### 1. **Configurazione HttpClient**
```json
{
  "ApiSettings": {
    "ProductServiceBaseUrl": "https://localhost:5003",
    "OrderServiceBaseUrl": "https://localhost:5007",
    "InventoryServiceBaseUrl": "https://localhost:5005"
  }
}
```

#### 2. **Dependency Injection Setup**
```csharp
builder.Services.AddHttpClient<ProductService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiSettings:ProductServiceBaseUrl"]);
    client.DefaultRequestHeaders.Add("User-Agent", "ShopVerse-CustomerWebsite/1.0");
});
```

#### 3. **Retry Policies**
```csharp
// Resilienza Network con Polly (future implementation)
builder.Services.AddHttpClient<ProductService>()
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());
```

---

## 📱 Responsive Design Strategy

### Breakpoints

| Dispositivo | Dimensione | Colonne Grid | Approccio |
|-------------|------------|--------------|-----------|
| **Mobile** | < 576px | 1-2 colonne | Stack verticale |
| **Tablet** | 576px - 768px | 2-3 colonne | Layout ibrido |
| **Laptop** | 768px - 1200px | 3-4 colonne | Grid completa |
| **Desktop** | > 1200px | 4-6 colonne | Layout espanso |

### Mobile-First CSS

```css
/* Mobile Base */
.product-grid {
    display: grid;
    grid-template-columns: 1fr;
    gap: 1rem;
}

/* Tablet Enhancement */
@media (min-width: 576px) {
    .product-grid {
        grid-template-columns: repeat(2, 1fr);
    }
}

/* Desktop Optimization */
@media (min-width: 992px) {
    .product-grid {
        grid-template-columns: repeat(4, 1fr);
    }
}
```

---

## ⚡ Performance Optimization

### Frontend Optimizations

#### 1. **Asset Optimization**
- **CSS Minification**: Compressione file CSS
- **JavaScript Bundling**: Consolidamento script
- **Image Optimization**: WebP, lazy loading
- **Font Optimization**: Subset fonts, preload

#### 2. **Caching Strategy**
```csharp
// Browser Caching Headers
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=31536000");
    }
});
```

#### 3. **Lazy Loading**
```javascript
// Intersection Observer per immagini
const imageObserver = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
        if (entry.isIntersecting) {
            const img = entry.target;
            img.src = img.dataset.src;
            imageObserver.unobserve(img);
        }
    });
});
```

### Backend Optimizations

#### 1. **HTTP Client Pooling**
```csharp
// Connection Pooling per microservices
builder.Services.AddHttpClient<ProductService>()
    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(30));
```

#### 2. **Response Caching**
```csharp
// Caching risposte API
[ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "category", "page" })]
public async Task<IActionResult> Products(string category = null)
```

---

## 🔒 Sicurezza & Privacy

### Misure di Sicurezza Implementate

#### 1. **HTTPS Enforcement**
```csharp
// Redirect HTTPS obbligatorio
app.UseHttpsRedirection();
app.UseHsts(); // HTTP Strict Transport Security
```

#### 2. **Session Security**
```csharp
// Cookie sicuri per sessioni
services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
    options.HttpOnly = HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.Always;
});
```

#### 3. **Content Security Policy**
```csharp
// CSP Headers per XSS protection
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Content-Security-Policy", 
        "default-src 'self'; style-src 'self' 'unsafe-inline'");
    await next();
});
```

#### 4. **Input Validation**
```csharp
// Data Annotations per validazione
[Required(ErrorMessage = "Il campo è obbligatorio")]
[StringLength(100, ErrorMessage = "Massimo 100 caratteri")]
public string ProductName { get; set; }
```

---

## 📊 Monitoring & Logging

### Sistema di Logging

#### 1. **Configurazione Logging**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "CustomerWebsite.Services": "Debug"
    }
  }
}
```

#### 2. **Logging Strutturato**
```csharp
// Logger injection nei servizi
_logger.LogInformation("Recupero prodotti in evidenza - Count: {Count}", count);
_logger.LogError(ex, "Errore durante chiamata API ProductService - Endpoint: {Endpoint}", endpoint);
```

#### 3. **Health Checks**
```csharp
// Health monitoring per dipendenze
builder.Services.AddHealthChecks()
    .AddCheck<ProductServiceHealthCheck>("product-service")
    .AddCheck<OrderServiceHealthCheck>("order-service");
```

### Metriche Performance

- **Response Time**: < 200ms per pagine statiche
- **API Calls**: < 500ms per chiamate microservizi
- **Page Load**: < 3s per First Contentful Paint
- **SEO Score**: > 90/100 Lighthouse

---

## 🚀 Deployment & DevOps

### Environment Configuration

#### 1. **Development**
```json
{
  "Environment": "Development",
  "ApiSettings": {
    "ProductServiceBaseUrl": "https://localhost:5003",
    "OrderServiceBaseUrl": "https://localhost:5007"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

#### 2. **Production**
```json
{
  "Environment": "Production",
  "ApiSettings": {
    "ProductServiceBaseUrl": "https://api.shopverse.com/products",
    "OrderServiceBaseUrl": "https://api.shopverse.com/orders"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
```

### Containerization Ready

```dockerfile
# Future Docker implementation
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["CustomerWebsite.csproj", "."]
RUN dotnet restore "./CustomerWebsite.csproj"
```

---

## 🧪 Testing Strategy

### Tipi di Test

#### 1. **Unit Tests**
```csharp
// Test servizi isolati
[Test]
public async Task GetFeaturedProducts_ReturnsExpectedProducts()
{
    // Arrange
    var mockHttpClient = CreateMockHttpClient();
    var productService = new ProductService(mockHttpClient);
    
    // Act
    var result = await productService.GetFeaturedProductsAsync(4);
    
    // Assert
    Assert.AreEqual(4, result.Count);
}
```

#### 2. **Integration Tests**
```csharp
// Test integrazione microservizi
[Test]
public async Task Homepage_LoadsSuccessfully()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/");
    
    // Assert
    response.EnsureSuccessStatusCode();
}
```

#### 3. **E2E Tests**
```javascript
// Cypress tests per user journey
describe('Shopping Cart Flow', () => {
    it('should add product to cart', () => {
        cy.visit('/');
        cy.get('[data-cy="product-card"]').first().click();
        cy.get('[data-cy="add-to-cart"]').click();
        cy.get('[data-cy="cart-count"]').should('contain', '1');
    });
});
```

---

## 📈 Roadmap & Future Enhancements

### Fase 2 - Q1 2026

#### Features Pianificate
- **Autenticazione Utente**: Login, registrazione, profilo utente
- **Wishlist**: Salvataggio prodotti preferiti
- **Reviews & Ratings**: Sistema recensioni prodotti
- **Search Avanzata**: Filtri dinamici, suggerimenti intelligenti

#### Miglioramenti Tecnici
- **Progressive Web App**: Service workers, offline support
- **GraphQL Integration**: Query ottimizzate per mobile
- **Micro-frontend**: Architettura modulare per team scalability
- **Real-time Updates**: SignalR per stock updates

### Fase 3 - Q2 2026

#### Advanced Features
- **Recommendation Engine**: ML-powered product suggestions
- **Multi-language Support**: i18n completo
- **Advanced Analytics**: Tracking comportamento utenti
- **A/B Testing**: Ottimizzazione conversioni

#### Performance Enhancements
- **CDN Integration**: Distribuzione globale asset
- **Database Caching**: Redis per session management
- **Image Optimization**: Next-gen formats, adaptive sizing
- **Bundle Optimization**: Tree shaking, code splitting

---

## 🤝 Contributing Guidelines

### Code Standards

#### 1. **Convenzioni Naming**
- **Controllers**: PascalCase + "Controller" suffix
- **Services**: PascalCase + "Service" suffix  
- **Models**: PascalCase, singular per entities
- **CSS Classes**: kebab-case, BEM methodology

#### 2. **Documentazione**
```csharp
/// <summary>
/// Recupera i prodotti in evidenza dal ProductService
/// </summary>
/// <param name="count">Numero massimo di prodotti da recuperare</param>
/// <returns>Lista di prodotti in evidenza</returns>
public async Task<List<ProductDto>> GetFeaturedProductsAsync(int count = 8)
```

#### 3. **Git Workflow**
- **Branch naming**: `feature/nome-funzionalità`
- **Commit messages**: Conventional Commits (it)
- **Pull Requests**: Template standardizzato
- **Code Review**: Minimo 2 approvazioni

### Development Setup

```bash
# Clone repository
git clone https://github.com/RizzatoSistemas/DistributedOrderSystem.git

# Navigate to frontend
cd frontend/customer-facing\ e-commerce/CustomerWebsite

# Restore packages
dotnet restore

# Run application
dotnet run
```

---

## 📞 Support & Contacts

### Team di Sviluppo

- **Lead Developer**: Rizzato Sistemas
- **Frontend Specialist**: [Da definire]
- **UX/UI Designer**: [Da definire]
- **DevOps Engineer**: [Da definire]

### Canali di Comunicazione

- **Email**: support@shopverse.com
- **Documentation**: Confluence interno
- **Issues**: GitHub Issues
- **Chat**: Microsoft Teams

### Risorse Utili

- **API Documentation**: Swagger UI su ogni microservizio
- **Design System**: Figma shared library
- **Performance Dashboard**: Grafana monitoring
- **Error Tracking**: Application Insights

---

## 📄 Licenza & Copyright

**Copyright © 2025 Rizzato Sistemas**

Questo progetto è proprietario e parte dell'architettura DistributedOrderSystem. 
Tutti i diritti sono riservati.

### Proprietà Intellettuale
- **Codice Sorgente**: Proprietario Rizzato Sistemas
- **Design Assets**: Licenza commerciale
- **Librerie Terze**: Rispettive licenze originali
- **Documentazione**: Creative Commons BY-NC-ND

---

## 📚 Bibliografia & Riferimenti

### Risorse Tecniche
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Bootstrap Documentation](https://getbootstrap.com/docs/5.3)
- [Microservices Patterns](https://microservices.io/)
- [Web Performance Best Practices](https://web.dev/performance)

### Standard & Guidelines
- [Microsoft C# Coding Conventions](https://docs.microsoft.com/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)
- [WCAG 2.1 Accessibility Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [HTTP Security Headers](https://owasp.org/www-project-secure-headers/)

---

**Documento creato il 17 Dicembre 2025**  
**Versione: 1.0**  
**Ultimo aggiornamento: 17 Dicembre 2025**

*Questo documento è parte integrante del progetto ShopVerse e viene aggiornato costantemente per riflettere l'evoluzione della piattaforma.*