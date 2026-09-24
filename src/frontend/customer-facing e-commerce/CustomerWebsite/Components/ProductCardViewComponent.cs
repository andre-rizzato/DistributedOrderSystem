using Microsoft.AspNetCore.Mvc;
using CustomerWebsite.Models;

namespace CustomerWebsite.Components;

/// <summary>
/// ViewComponent for rendering the product card
/// Used to create reusable product cards throughout the application
/// </summary>
public class ProductCardViewComponent : ViewComponent
{
    /// <summary>
    /// Component invocation method
    /// </summary>
    /// <param name="product">The product to display</param>
    /// <returns>View with the product model</returns>
    public IViewComponentResult Invoke(ProductDisplayModel product)
    {
        // Validate the incoming parameter
        if (product == null)
        {
            throw new ArgumentNullException(nameof(product), "Product cannot be null");
        }

        // Pass the model to the component's view
        return View(product);
    }
}
