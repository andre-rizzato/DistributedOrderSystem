# ViewComponent Flow & Usage Guide

> Updated to reflect the real code. Previous versions described an asynchronous `ProductCardViewComponent` with dependency injection, invoked via `@await Component.InvokeAsync("ProductCard", product)`. The real component is synchronous, has no DI, and is invoked with the Tag Helper syntax (`<vc:product-card>`), not an explicit call to the `Component.InvokeAsync` method. Missing pages were also found that make some of the flows described below unreachable — see the "⚠️" notes.

## Overview
This guide explains how ViewComponents work in the ShopVerse Customer Website, specifically the real `ProductCardViewComponent` implementation.

---

## **1. ViewComponent Class** (`ProductCardViewComponent.cs`) — real code

```csharp
public class ProductCardViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(ProductDisplayModel product)
    {
        if (product == null)
        {
            throw new ArgumentNullException(nameof(product), "The product cannot be null");
        }

        return View(product);
    }
}
```

### **What it actually does:**
- It's **synchronous** (`Invoke`, not `InvokeAsync`) — there are no asynchronous calls, no I/O.
- **It injects no service** — it has no constructor with `IProductService` or anything else. It receives the `ProductDisplayModel` already prepared by the caller; it doesn't fetch data itself.
- Throws `ArgumentNullException` if the product passed in is `null` — no silent fallback.

⚠️ Previous versions of this document showed an example with `InvokeAsync(Guid productId)` and `IProductService` injection to demonstrate "what could be done" — that example **doesn't match the code present in the repository**. If the component were to be made to fetch its own data in the future, that would be a change to implement, not something already present.

### **When it's invoked:**
ASP.NET Core discovers the component by naming convention (`[Name]ViewComponent` → `ProductCard`), but **the way it's invoked in the real code is via a Tag Helper**, not via an explicit call to `Component.InvokeAsync(...)`:

```razor
<vc:product-card product="@product"></vc:product-card>
```

This works because `Views/_ViewImports.cshtml` includes:
```razor
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
@addTagHelper *, CustomerWebsite
```
The second line registers the Tag Helpers (including the View Component Tag Helper) defined in the `CustomerWebsite` assembly. The `@await Component.InvokeAsync("ProductCard", product)` form is still a valid ASP.NET Core API and would produce the same result, but **it's not the one used in the code** — don't look for it in the existing views, it isn't there.

---

## **2. ViewComponent View** (`Default.cshtml`)

Real path: `Views/Shared/Components/ProductCard/Default.cshtml`, typed as `@model CustomerWebsite.Models.ProductDisplayModel`.

### **What it does (confirmed from the code):**
- Pure presentation: image (falling back to a placeholder generated via `via.placeholder.com` if `Model.Images` is empty), discount/bestseller badge, rating stars, link to the product detail page.
- The link to the product detail page points to `/Product/Details/@Model.ProductId`.

⚠️ **`ProductController.Details(Guid id)` exists, but no `Views/Product/Details.cshtml` view exists** in the project. Clicking on any product card produces a runtime error (`InvalidOperationException: The view 'Details' was not found`), not a working detail page. This isn't a problem with the ViewComponent itself — the card renders correctly — but with the fact that the destination page doesn't exist yet.

---

## **3. Usage in Views** (`Index.cshtml`) — real flow

Unlike what was previously described (two alternative "scenarios" with different rendering), **`Views/Home/Index.cshtml` has a single code path** that applies to both the real data and the fallback:

```razor
@{
    var productsToDisplay = (Model?.FeaturedProducts != null && Model.FeaturedProducts.Any())
        ? Model.FeaturedProducts
        : new List<ProductDisplayModel>
    {
        new ProductDisplayModel { ProductId = Guid.NewGuid(), Name = "Apple iPhone 15 Pro 128GB", Price = 1229.00m, ... },
        // ... 7 more hardcoded Apple/electronics products, with a new Guid on every render
    };
}

@foreach(var product in productsToDisplay)
{
    <vc:product-card product="@product"></vc:product-card>
}
```

- **If the backend responds** (`HomeController.Index` successfully called `ProductService.GetFeaturedProductsAsync`): `productsToDisplay` contains the real products.
- **If the backend doesn't respond or returns empty**: `productsToDisplay` is a **hardcoded list of 8 Apple/electronics products** (iPhone, MacBook Air, AirPods, iPad, Apple Watch, a Canon EOS R6, some Sony WH-1000XM5, a Nintendo Switch) with a `Guid.NewGuid()` generated on every page load — these are not real product ids, and clicking them still runs into the missing `Product/Details` view problem described above.
- In **both cases** the same `@foreach` runs with the same `<vc:product-card>` Tag Helper — there aren't two alternative markup blocks as previous versions of this document suggested.

---

## **Complete Flow Diagram (real):**

```
┌─────────────────────────────────────────────────────────────┐
│ 1. USER REQUESTS PAGE                                        │
│    GET /Home/Index  (or GET /  — default route)              │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ 2. HOMECONTROLLER.Index()                                    │
│    - Calls IProductService (GetFeaturedProductsAsync, etc.)  │
│    - On exception: catches, logs, returns an EMPTY            │
│      HomePageViewModel (doesn't propagate the error)          │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ 3. INDEX.CSHTML — a single rendering path                     │
│    productsToDisplay = real FeaturedProducts                  │
│                         OR 8 hardcoded Apple products          │
│    @foreach(var product in productsToDisplay)                 │
│    {                                                          │
│        <vc:product-card product="@product"></vc:product-card>│
│    }                                                          │
└────────────────────────┬────────────────────────────────────┘
                         │ (Tag Helper resolves the component)
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ 4. PRODUCTCARDVIEWCOMPONENT.Invoke(product) — synchronous      │
│    - No DI, no I/O                                            │
│    - throws if product is null                                │
│    - return View(product)                                     │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ 5. DEFAULT.CSHTML (Component View)                            │
│    - Renders image/price/rating/badge                        │
│    - Link "/Product/Details/{id}" — ⚠️ no destination view,     │
│      see above                                                 │
└────────────────────────┬────────────────────────────────────┘
                         │
                         ▼
┌─────────────────────────────────────────────────────────────┐
│ 6. FINAL HTML SENT TO THE BROWSER                             │
│    Cards render correctly; click → runtime error              │
└─────────────────────────────────────────────────────────────┘
```

---

## **When to Use ViewComponents vs Partial Views:**

This table is conceptual (a general ASP.NET Core explanation) and remains valid regardless of this specific project's status:

| Feature | **ViewComponent** | **Partial View** |
|---------|------------------|------------------|
| **Logic** | Can contain logic & inject services | Pure presentation only |
| **Reusability** | Highly reusable across entire app | Reusable but simpler |
| **Data fetching** | ✅ Can fetch its own data (our `ProductCardViewComponent` chooses not to) | ❌ Needs data passed from parent |
| **Testing** | ✅ Easily testable (it's a class) | ⚠️ Harder to test |
| **Use case** | Complex, self-contained UI elements | Simple UI fragments |

⚠️ Note that **no automated test exists today** for `ProductCardViewComponent` or for the rest of `CustomerWebsite` — the "testability" in the table is an architectural property, not a guarantee that it's actually been exercised.

---

## **Other Places to Use Your ProductCard Component:**

Previous versions of this document listed Search Results, Category, Wishlist, and Related Products as pages where the component is "already" used. **None of these views exist in the project today**:

```
Views/
├── _ViewImports.cshtml, _ViewStart.cshtml
├── Home/
│   ├── Index.cshtml      ← the only place <vc:product-card> is genuinely used
│   └── Privacy.cshtml
└── Shared/
    ├── _Layout.cshtml, _Layout.cshtml.css, _ValidationScriptsPartial.cshtml, Error.cshtml
    └── Components/ProductCard/Default.cshtml
```

`Views/Product/Details.cshtml`, `Views/Product/Compare.cshtml`, `Views/Cart/Index.cshtml`, `Views/Checkout/Index.cshtml`, `Views/Home/Search.cshtml`, `Views/Home/Categories.cshtml`, `Views/Home/Category.cshtml`, `Views/Home/About.cshtml`, `Views/Home/Contact.cshtml`, `Views/Home/CustomerService.cshtml`, and the `_ProductReviews` partial required by `ProductController.LoadReviews` don't exist. The corresponding controllers (`ProductController`, `CartController`, `CheckoutController`, and several `HomeController` actions) exist and contain working C#-side logic, but **calling them today produces a "view not found" runtime error**, not a page — so the ProductCard component can't yet be "reused" on those pages because the pages themselves don't exist.

The following code remains a correct example of **how** the component would be invoked elsewhere once those views are written — not a list of already-working usages:

```razor
@* Hypothetical, not present today: Search Results Page *@
@foreach(var product in Model.SearchResults)
{
    <vc:product-card product="@product"></vc:product-card>
}
```

---

## **Key Benefits (conceptually valid, not all exploited today):**

1. **Single Source of Truth**: true for the one page that exists (Home) — a single component, a single place to change the card's styling.
2. **Maintainability**: same note as above.
3. **Testability**: possible in principle; no test written today.
4. **Separation of Concerns**: respected in the real code — the component is purely synchronous/presentational.
5. **Reusability**: potential, but not yet exploited: today the component is used in a single view.

---

## **File Structure (real):**

```
CustomerWebsite/
├── Components/
│   └── ProductCardViewComponent.cs          # synchronous, no DI
├── Views/
│   ├── _ViewImports.cshtml                  # enables @addTagHelper *, CustomerWebsite
│   ├── Shared/
│   │   └── Components/
│   │       └── ProductCard/
│   │           └── Default.cshtml           # presentation, links to /Product/Details/{id}
│   └── Home/
│       └── Index.cshtml                     # the only real consumer of the component
```

This structure follows ASP.NET Core conventions and enables automatic component discovery — the infrastructure part is correct and working. The gap is downstream: the pages that should reuse the component haven't been written yet.
