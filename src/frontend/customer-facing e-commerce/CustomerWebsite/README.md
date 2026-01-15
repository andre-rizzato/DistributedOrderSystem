# CustomerWebsite - ShopVerse

## Descrizione
CustomerWebsite è la piattaforma e-commerce **ShopVerse** sviluppata con ASP.NET Core MVC. Offre un'esperienza shopping moderna e intuitiva con design responsivo, palette colori personalizzata e funzionalità avanzate per il commercio elettronico.

## Caratteristiche

### 🎨 **Design e UI Moderna**
- Design unico ShopVerse con palette colori distintiva
- Gradiente principale: Viola (#7C3AED), Indaco (#4F46E5), Teal (#14B8A6)
- Design responsivo con Bootstrap 5 e animazioni CSS avanzate
- Icone FontAwesome per UX moderna e intuitiva
- Effetti hover e transizioni fluide per interattività ottimale

### 🛍️ **Funzionalità E-commerce ShopVerse**
- Homepage con design hero moderno e call-to-action accattivanti
- Sistema di navigazione per categorie con icone intuitive
- Ricerca intelligente con autocomplete e suggerimenti in tempo reale
- Carrello della spesa con persistenza sessione e AJAX updates
- Lista desideri per salvare prodotti preferiti
- Sistema checkout multi-step con validazione avanzata
- Gestione account utente completa con profili personalizzati
- Sistema recensioni e valutazioni prodotti con stelle interattive

### 🏗️ **Architettura Tecnica**
- **Framework**: ASP.NET Core MVC (.NET 9.0) con pattern moderno
- **Design Pattern**: Model-View-Controller con separation of concerns
- **Dependency Injection**: Configurazione completa per tutti i servizi
- **Session Management**: Gestione sicura per carrello e preferenze utente
- **HTTP Client**: Integrazione seamless con architettura microservizi

### 📁 **Struttura Progetto**

```
CustomerWebsite/
├── Controllers/
│   ├── HomeController.cs          # Homepage, ricerca, categorie
│   ├── ProductController.cs       # Dettagli prodotto, recensioni
│   ├── CartController.cs          # Gestione carrello
│   └── CheckoutController.cs      # Processo di acquisto
├── Models/
│   ├── ProductModels.cs           # Modelli prodotti e ricerca
│   ├── ShoppingModels.cs          # Carrello e wishlist
│   ├── AccountModels.cs           # Utenti e autenticazione
│   ├── OrderModels.cs             # Ordini e checkout
│   └── ErrorViewModel.cs          # Gestione errori
├── Views/
│   ├── Shared/
│   │   └── _Layout.cshtml         # Layout principale ShopVerse
│   ├── Home/
│   │   └── Index.cshtml           # Homepage con prodotti
│   └── _ViewImports.cshtml        # Import namespaces
├── Services/
│   ├── ProductService.cs          # Integrazione ProductService
│   ├── ShoppingCartService.cs     # Gestione carrello
│   └── OrderService.cs            # Gestione ordini
├── wwwroot/
│   ├── css/
│   │   ├── shopverse.css           # Stili ShopVerse personalizzati
│   │   └── site.css                # Stili base applicazione
│   ├── js/
│   │   ├── shopverse.js            # Funzionalità JavaScript avanzate
│   │   └── site.js                 # Script base
│   └── images/                     # Asset immagini e icone
└── Program.cs                     # Configurazione applicazione
```

### 🎯 **Modelli di Dati**

#### **ProductModels.cs**
- `ProductDisplayModel`: Visualizzazione prodotti
- `ProductSearchModel`: Ricerca e filtri
- `CategoryModel`: Categorie prodotti
- `ProductImageModel`: Gestione immagini
- `ProductReviewModel`: Sistema recensioni

#### **ShoppingModels.cs**
- `ShoppingCartModel`: Carrello acquisti
- `CartItemModel`: Elementi carrello
- `WishlistModel`: Lista desideri
- `HomePageViewModel`: Dati homepage

#### **AccountModels.cs**
- `LoginModel`: Autenticazione
- `RegisterModel`: Registrazione
- `UserProfileModel`: Profilo utente
- `AddressModel`: Indirizzi spedizione

#### **OrderModels.cs**
- `OrderModel`: Gestione ordini
- `CheckoutModel`: Processo acquisto
- `OrderHistoryModel`: Storico ordini

### 🔧 **Servizi Integrati**

#### **ProductService**
- Ricerca prodotti
- Gestione categorie
- Sistema recensioni
- Comparazione prodotti

#### **ShoppingCartService**
- Gestione carrello sessione
- Calcolo totali
- Lista desideri
- Promozioni e sconti

#### **OrderService**
- Processo checkout
- Gestione spedizioni
- Codici promozionali
- Storico ordini

### 🌐 **Funzionalità Web**

#### **Homepage ShopVerse**
- Hero banner con gradiente moderno e typography accattivante
- 8 categorie principali con icone FontAwesome intuitive
- Sezione prodotti in evidenza con placeholder realistici
- Banner promozionali con animazioni CSS avanzate
- Ricerca intelligente con dropdown suggerimenti

#### **Navigazione Moderna**
- Header ShopVerse con gradiente viola-indaco-teal
- Menu categorie con hover effects e transitions
- Ricerca con autocomplete e debouncing ottimizzato
- Carrello con contatore animato e badge notifiche
- Footer completo con design a gradiente scuro

#### **Interattività**
- AJAX per carrello
- Ricerca in tempo reale
- Notifiche utente
- Animazioni CSS
- Design responsivo

### 🚀 **Setup e Avvio**

#### **Prerequisiti**
- .NET 9.0 SDK
- Visual Studio 2022 o VS Code
- Browser moderno

#### **Avvio Locale**
```bash
cd "src/frontend/customer-facing e-commerce/CustomerWebsite"
dotnet restore
dotnet build
dotnet run
```

Il sito sarà disponibile su: `http://localhost:5246`

#### **Integrazione Microservizi**
Per funzionalità complete, avviare i microservizi:
- ProductService (porta 5001)
- OrderService (porta 5002)
- InventoryService (porta 5003)
- PaymentService (porta 5004)
- NotificationService (porta 5005)
- ChatbotService (porta 5007)

### 🎨 **Personalizzazione Design ShopVerse**

#### **Palette Colori Distintiva**
Il file `wwwroot/css/shopverse.css` implementa:
- **Primari**: Viola (#7C3AED), Indaco (#4F46E5), Blu (#3B82F6)
- **Accenti**: Teal (#14B8A6), Verde (#10B981), Giallo (#FDE047)
- **Gradienti**: Combinazioni multicolori per header, hero e CTA
- **Hover Effects**: Trasformazioni 3D e color transitions
- **Animazioni**: Keyframes personalizzate per sparkle e pulse

#### **JavaScript Interattivo**
Il file `wwwroot/js/shopverse.js` include:
- Sistema ricerca con debouncing intelligente (300ms)
- Gestione carrello AJAX con feedback visuale immediato
- Notifiche toast con auto-dismiss dopo 4 secondi
- Validazione form in tempo reale con error styling
- Lazy loading immagini per performance ottimizzate

### 🔗 **Integrazione API**

#### **Endpoints Utilizzati**
- `GET /api/products` - Lista prodotti
- `GET /api/products/{id}` - Dettagli prodotto
- `POST /api/cart/add` - Aggiungi al carrello
- `GET /api/categories` - Categorie
- `POST /api/orders` - Crea ordine

### 🛡️ **Sicurezza**
- Anti-forgery tokens
- Validazione input
- Gestione errori
- Session security
- HTTPS ready

### 📱 **Responsive Design**
- Mobile-first approach
- Breakpoint Bootstrap
- Touch-friendly UI
- Immagini ottimizzate
- Performance ottimizzata

### 🧪 **Testing**
Il progetto include:
- Placeholder per unit test
- Mock data per sviluppo
- Error handling completo
- Logging integrato

### 📈 **Performance**
- Lazy loading immagini
- Minificazione CSS/JS
- CDN per Bootstrap/FontAwesome
- Cache HTTP headers
- Compressione response

### 🎯 **Caratteristiche Uniche ShopVerse**
- **Design Distintivo**: Palette colori originale con gradienti moderni
- **UX Ottimizzata**: Animazioni fluide e micro-interazioni accattivanti  
- **Performance**: Lazy loading, debouncing e ottimizzazioni avanzate
- **Accessibilità**: Design inclusive con focus su usabilità universale
- **SEO Ready**: Meta tags ottimizzati e struttura HTML semantica

### 🚀 **Prossimi Sviluppi ShopVerse**
- [ ] Integrazione ChatBot AI widget con NLP avanzato
- [ ] Sistema notifiche push real-time con SignalR
- [ ] Progressive Web App (PWA) per mobile experience
- [ ] Analytics avanzate con dashboard amministratore
- [ ] A/B testing framework per ottimizzazione conversioni
- [ ] Sistema recommendation AI personalizzato

## Tecnologie Avanzate ShopVerse

- **Backend**: ASP.NET Core MVC 9.0 con architettura scalabile
- **Frontend**: HTML5 semantico, CSS3 con custom properties, JavaScript ES6+
- **Styling**: Bootstrap 5.3 customizzato con design system proprietario
- **Icons**: FontAwesome 6.0 per iconografia professionale
- **HTTP**: HttpClientFactory per comunicazioni microservizi ottimizzate
- **Session**: ASP.NET Core Session con configurazione sicura avanzata
- **Development**: Visual Studio 2022 con hot reload e IntelliSense

## Author
**Rizzato Sistemas** - ShopVerse E-commerce Platform per architettura distribuita

## Licenza
Progetto proprietario - Implementazione avanzata per sistemi e-commerce enterprise

---

*ShopVerse - La tua piattaforma e-commerce del futuro, dove tecnologia avanzata incontra design eccezionale per creare esperienze shopping uniche e memorabili.*