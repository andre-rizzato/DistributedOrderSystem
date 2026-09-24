# CustomerWebsite - ShopVerse

## Description
CustomerWebsite is the **ShopVerse** e-commerce platform built with ASP.NET Core MVC. It offers a modern, intuitive shopping experience with responsive design, a custom color palette, and advanced e-commerce features.

## Features

### 🎨 **Modern Design and UI**
- Unique ShopVerse design with a distinctive color palette
- Main gradient: Purple (#7C3AED), Indigo (#4F46E5), Teal (#14B8A6)
- Responsive design with Bootstrap 5 and advanced CSS animations
- FontAwesome icons for a modern, intuitive UX
- Hover effects and smooth transitions for optimal interactivity

### 🛍️ **ShopVerse E-commerce Features**
- Homepage with a modern hero design and engaging calls-to-action
- Category navigation system with intuitive icons
- Smart search with autocomplete and real-time suggestions
- Shopping cart with session persistence and AJAX updates
- Wishlist for saving favorite products
- Multi-step checkout system with advanced validation
- Complete user account management with personalized profiles
- Product review and rating system with interactive stars

### 🏗️ **Technical Architecture**
- **Framework**: ASP.NET Core MVC (.NET 9.0) with a modern pattern
- **Design Pattern**: Model-View-Controller with separation of concerns
- **Dependency Injection**: Full configuration for all services
- **Session Management**: Secure handling for cart and user preferences
- **HTTP Client**: Seamless integration with the microservices architecture

### 📁 **Project Structure**

```
CustomerWebsite/
├── Controllers/
│   ├── HomeController.cs          # Homepage, search, categories
│   ├── ProductController.cs       # Product details, reviews
│   ├── CartController.cs          # Cart management
│   └── CheckoutController.cs      # Purchase process
├── Models/
│   ├── ProductModels.cs           # Product and search models
│   ├── ShoppingModels.cs          # Cart and wishlist
│   ├── AccountModels.cs           # Users and authentication
│   ├── OrderModels.cs             # Orders and checkout
│   └── ErrorViewModel.cs          # Error handling
├── Views/
│   ├── Shared/
│   │   └── _Layout.cshtml         # Main ShopVerse layout
│   ├── Home/
│   │   └── Index.cshtml           # Homepage with products
│   └── _ViewImports.cshtml        # Namespace imports
├── Services/
│   ├── ProductService.cs          # ProductService integration
│   ├── ShoppingCartService.cs     # Cart management
│   └── OrderService.cs            # Order management
├── wwwroot/
│   ├── css/
│   │   ├── shopverse.css           # Custom ShopVerse styles
│   │   └── site.css                # Base application styles
│   ├── js/
│   │   ├── shopverse.js            # Advanced JavaScript functionality
│   │   └── site.js                 # Base script
│   └── images/                     # Image and icon assets
└── Program.cs                     # Application configuration
```

### 🎯 **Data Models**

#### **ProductModels.cs**
- `ProductDisplayModel`: Product display
- `ProductSearchModel`: Search and filters
- `CategoryModel`: Product categories
- `ProductImageModel`: Image handling
- `ProductReviewModel`: Review system

#### **ShoppingModels.cs**
- `ShoppingCartModel`: Shopping cart
- `CartItemModel`: Cart items
- `WishlistModel`: Wishlist
- `HomePageViewModel`: Homepage data

#### **AccountModels.cs**
- `LoginModel`: Authentication
- `RegisterModel`: Registration
- `UserProfileModel`: User profile
- `AddressModel`: Shipping addresses

#### **OrderModels.cs**
- `OrderModel`: Order management
- `CheckoutModel`: Purchase process
- `OrderHistoryModel`: Order history

### 🔧 **Integrated Services**

#### **ProductService**
- Product search
- Category management
- Review system
- Product comparison

#### **ShoppingCartService**
- Session cart management
- Total calculation
- Wishlist
- Promotions and discounts

#### **OrderService**
- Checkout process
- Shipping management
- Promo codes
- Order history

### 🌐 **Web Features**

#### **ShopVerse Homepage**
- Hero banner with a modern gradient and eye-catching typography
- 8 main categories with intuitive FontAwesome icons
- Featured products section with realistic placeholders
- Promotional banners with advanced CSS animations
- Smart search with a suggestions dropdown

#### **Modern Navigation**
- ShopVerse header with a purple-indigo-teal gradient
- Category menu with hover effects and transitions
- Search with autocomplete and optimized debouncing
- Cart with animated counter and notification badge
- Full footer with a dark gradient design

#### **Interactivity**
- AJAX for the cart
- Real-time search
- User notifications
- CSS animations
- Responsive design

### 🚀 **Setup and Startup**

#### **Prerequisites**
- .NET 9.0 SDK
- Visual Studio 2022 or VS Code
- Modern browser

#### **Local Startup**
```bash
cd "src/frontend/customer-facing e-commerce/CustomerWebsite"
dotnet restore
dotnet build
dotnet run
```

The site will be available at: `http://localhost:5246`

#### **Microservices Integration**
For full functionality, start the microservices:
- ProductService (port 5001)
- OrderService (port 5002)
- InventoryService (port 5003)
- PaymentService (port 5004)
- NotificationService (port 5005)
- ChatbotService (port 5007)

### 🎨 **ShopVerse Design Customization**

#### **Distinctive Color Palette**
The `wwwroot/css/shopverse.css` file implements:
- **Primary**: Purple (#7C3AED), Indigo (#4F46E5), Blue (#3B82F6)
- **Accents**: Teal (#14B8A6), Green (#10B981), Yellow (#FDE047)
- **Gradients**: Multicolor combinations for headers, hero sections, and CTAs
- **Hover Effects**: 3D transforms and color transitions
- **Animations**: Custom keyframes for sparkle and pulse effects

#### **Interactive JavaScript**
The `wwwroot/js/shopverse.js` file includes:
- Search system with smart debouncing (300ms)
- AJAX cart management with immediate visual feedback
- Toast notifications with auto-dismiss after 4 seconds
- Real-time form validation with error styling
- Image lazy loading for optimized performance

### 🔗 **API Integration**

#### **Endpoints Used**
- `GET /api/products` - Product list
- `GET /api/products/{id}` - Product details
- `POST /api/cart/add` - Add to cart
- `GET /api/categories` - Categories
- `POST /api/orders` - Create order

### 🛡️ **Security**
- Anti-forgery tokens
- Input validation
- Error handling
- Session security
- HTTPS ready

### 📱 **Responsive Design**
- Mobile-first approach
- Bootstrap breakpoints
- Touch-friendly UI
- Optimized images
- Optimized performance

### 🧪 **Testing**
The project includes:
- Placeholder for unit tests
- Mock data for development
- Complete error handling
- Integrated logging

### 📈 **Performance**
- Image lazy loading
- CSS/JS minification
- CDN for Bootstrap/FontAwesome
- HTTP cache headers
- Response compression

### 🎯 **Unique ShopVerse Features**
- **Distinctive Design**: Original color palette with modern gradients
- **Optimized UX**: Smooth animations and engaging micro-interactions
- **Performance**: Lazy loading, debouncing, and advanced optimizations
- **Accessibility**: Inclusive design focused on universal usability
- **SEO Ready**: Optimized meta tags and semantic HTML structure

### 🚀 **Upcoming ShopVerse Development**
- [ ] AI ChatBot widget integration with advanced NLP
- [ ] Real-time push notification system with SignalR
- [ ] Progressive Web App (PWA) for the mobile experience
- [ ] Advanced analytics with an admin dashboard
- [ ] A/B testing framework for conversion optimization
- [ ] Personalized AI recommendation system

## Advanced ShopVerse Technologies

- **Backend**: ASP.NET Core MVC 9.0 with a scalable architecture
- **Frontend**: Semantic HTML5, CSS3 with custom properties, JavaScript ES6+
- **Styling**: Customized Bootstrap 5.3 with a proprietary design system
- **Icons**: FontAwesome 6.0 for professional iconography
- **HTTP**: HttpClientFactory for optimized microservice communication
- **Session**: ASP.NET Core Session with advanced secure configuration
- **Development**: Visual Studio 2022 with hot reload and IntelliSense

## Author
**Rizzato Sistemas** - ShopVerse E-commerce Platform for distributed architecture

## License
Proprietary project - Advanced implementation for enterprise e-commerce systems

---

*ShopVerse - Your e-commerce platform of the future, where advanced technology meets exceptional design to create unique and memorable shopping experiences.*
