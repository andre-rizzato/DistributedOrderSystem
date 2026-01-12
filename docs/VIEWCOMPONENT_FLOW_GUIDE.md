# ViewComponent Flow & Usage Guide

## Overview
This guide explains how ViewComponents work in the ShopVerse Customer Website, specifically the ProductCard ViewComponent implementation.

---

## **1. ViewComponent Class** (`ProductCardViewComponent.cs`)

### **When it's invoked:**
- When you call `@await Component.InvokeAsync("ProductCard", product)` in any view
- ASP.NET Core automatically discovers it by the naming convention: `[Name]ViewComponent`

### **What it does:**
- Acts as a **mini-controller** for the component
- Receives data (the `ProductDisplayModel` parameter)
- Can inject services (e.g., `IProductService`, database context) if you need to fetch additional data
- Validates input
- Passes data to the view

### **Example with service injection:**
```csharp
public class ProductCardViewComponent : ViewComponent
{
    private readonly IProductService _productService;
    
    public ProductCardViewComponent(IProductService productService)
    {
        _productService = productService;
    }
    
    public async Task<IViewComponentResult> InvokeAsync(Guid productId)
    {
        // Can fetch data from database/API
        var product = await _productService.GetProductByIdAsync(productId);
        return View(product);
    }
}
```

---

## **2. ViewComponent View** (`Default.cshtml`)

### **When it's used:**
- Automatically rendered by the ViewComponent class
- The `Default.cshtml` name is the convention (like `Index` for controllers)

### **What it does:**
- Pure **presentation logic** only
- Receives the model from the ViewComponent class
- Renders the HTML/Razor markup
- Should NOT contain business logic or data fetching

---

## **3. Usage in Views** (`Index.cshtml`)

### **Scenario A: Database-driven (Dynamic Products)**
```razor
@if(Model?.FeaturedProducts != null && Model.FeaturedProducts.Any())
{
    @foreach(var product in Model.FeaturedProducts)
    {
        @await Component.InvokeAsync("ProductCard", product)
    }
}
```
- **Controller** fetches products from database → passes to view via ViewModel
- **View** loops through products and invokes component for each
- **Component** receives each product and renders it

### **Scenario B: Static Fallback**
```razor
else
{
    <!-- Hard-coded HTML when backend is unavailable -->
    <div class="col-lg-3 col-md-6">...</div>
}
```

---

## **Complete Flow Diagram:**

```
┌─────────────────────────────────────────────────────────────┐
│ 1. USER REQUESTS PAGE                                       │
│    GET /Home/Index                                          │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. HOMECONTROLLER                                           │
│    - Calls ProductService.GetFeaturedProducts()             │
│    - Creates HomePageViewModel                              │
│    - Passes to Index.cshtml                                 │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. INDEX.CSHTML (View)                                      │
│    @model HomePageViewModel                                 │
│    @foreach(var product in Model.FeaturedProducts)          │
│    {                                                         │
│        @await Component.InvokeAsync("ProductCard", product) │
│    }                                                         │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼ (For each product)
┌─────────────────────────────────────────────────────────────┐
│ 4. PRODUCTCARDVIEWCOMPONENT.cs                              │
│    - Receives ProductDisplayModel                           │
│    - Validates it                                           │
│    - Returns View(product)                                  │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. DEFAULT.CSHTML (Component View)                          │
│    - Renders product card HTML                              │
│    - Shows image, price, rating, buttons                    │
│    - Returns HTML to parent view                            │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ 6. FINAL HTML SENT TO BROWSER                               │
│    Multiple <div class="product-card">...</div> rendered    │
└─────────────────────────────────────────────────────────────┘
```

---

## **When to Use ViewComponents vs Partial Views:**

| Feature | **ViewComponent** | **Partial View** |
|---------|------------------|------------------|
| **Logic** | Can contain logic & inject services | Pure presentation only |
| **Reusability** | Highly reusable across entire app | Reusable but simpler |
| **Data fetching** | ✅ Can fetch its own data | ❌ Needs data passed from parent |
| **Testing** | ✅ Easily testable (it's a class) | ⚠️ Harder to test |
| **Use case** | Complex, self-contained UI elements | Simple UI fragments |

---

## **Other Places to Use Your ProductCard Component:**

```razor
@* Search Results Page *@
@foreach(var product in Model.SearchResults)
{
    @await Component.InvokeAsync("ProductCard", product)
}

@* Category Page *@
@foreach(var product in Model.CategoryProducts)
{
    @await Component.InvokeAsync("ProductCard", product)
}

@* Wishlist Page *@
@foreach(var product in Model.WishlistProducts)
{
    @await Component.InvokeAsync("ProductCard", product)
}

@* Related Products (on Product Detail page) *@
@foreach(var product in Model.RelatedProducts)
{
    @await Component.InvokeAsync("ProductCard", product)
}
```

---

## **Key Benefits:**

1. **Single Source of Truth**: One component, used everywhere! Any styling or functionality changes happen in one place.

2. **Maintainability**: Update the product card design once, and it updates across all pages.

3. **Testability**: ViewComponents are C# classes that can be unit tested independently.

4. **Separation of Concerns**: Logic stays in the component class, presentation stays in the view.

5. **Reusability**: Works across Home, Search, Category, Wishlist, and any other page that displays products.

---

## **File Structure:**

```
CustomerWebsite/
├── Components/
│   └── ProductCardViewComponent.cs          # ViewComponent logic
├── Views/
│   ├── Shared/
│   │   └── Components/
│   │       └── ProductCard/
│   │           └── Default.cshtml           # ViewComponent presentation
│   └── Home/
│       └── Index.cshtml                     # Uses the component
```

This structure follows ASP.NET Core conventions and enables automatic component discovery.
