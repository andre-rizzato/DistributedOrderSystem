using Microsoft.AspNetCore.Mvc;
using CustomerWebsite.Models;
using CustomerWebsite.Services;

namespace CustomerWebsite.Controllers;

/// <summary>
/// Controller for the checkout process
/// </summary>
public class CheckoutController : Controller
{
    private readonly IShoppingCartService _cartService;
    private readonly IOrderService _orderService;
    private readonly ILogger<CheckoutController> _logger;

    public CheckoutController(
        IShoppingCartService cartService,
        IOrderService orderService,
        ILogger<CheckoutController> logger)
    {
        _cartService = cartService;
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Main checkout page
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            var sessionId = HttpContext.Session.Id;
            var cart = await _cartService.GetCartAsync(sessionId);

            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction("Index", "Cart");
            }

            var model = new CheckoutModel
            {
                Cart = cart,
                CurrentStep = CheckoutStep.ReviewCart
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing checkout");
            return RedirectToAction("Index", "Cart");
        }
    }

    /// <summary>
    /// Step 2: Shipping address
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ShippingAddress(CheckoutModel model)
    {
        try
        {
            var sessionId = HttpContext.Session.Id;
            model.Cart = await _cartService.GetCartAsync(sessionId);

            if (ModelState.IsValid)
            {
                // Load the available shipping options
                model.AvailableShippingOptions = await _orderService.GetShippingOptionsAsync(
                    model.ShippingAddress,
                    model.Cart.Items);

                model.CurrentStep = CheckoutStep.ShippingMethod;

                TempData["CheckoutModel"] = Newtonsoft.Json.JsonConvert.SerializeObject(model);
                return View("Index", model);
            }

            model.CurrentStep = CheckoutStep.ShippingAddress;
            return View("Index", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating the shipping address");
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Step 3: Shipping method
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ShippingMethod(CheckoutModel model, Guid shippingOptionId)
    {
        try
        {
            var sessionId = HttpContext.Session.Id;
            model.Cart = await _cartService.GetCartAsync(sessionId);

            // Find the selected shipping option
            model.AvailableShippingOptions = await _orderService.GetShippingOptionsAsync(
                model.ShippingAddress,
                model.Cart.Items);

            var selectedOption = model.AvailableShippingOptions
                .FirstOrDefault(o => o.ShippingOptionId == shippingOptionId);

            if (selectedOption != null)
            {
                model.SelectedShippingOption = selectedOption;
                model.CurrentStep = CheckoutStep.PaymentMethod;

                TempData["CheckoutModel"] = Newtonsoft.Json.JsonConvert.SerializeObject(model);
                return View("Index", model);
            }

            ModelState.AddModelError("", "Select a valid shipping method.");
            model.CurrentStep = CheckoutStep.ShippingMethod;
            return View("Index", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting the shipping method");
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Step 4: Payment method
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PaymentMethod(CheckoutModel model)
    {
        try
        {
            var sessionId = HttpContext.Session.Id;
            model.Cart = await _cartService.GetCartAsync(sessionId);

            if (ModelState.IsValid)
            {
                // Compute the final taxes
                model.TaxAmount = model.Cart.Subtotal * 0.22m; // Italian VAT 22%

                model.CurrentStep = CheckoutStep.ReviewOrder;

                TempData["CheckoutModel"] = Newtonsoft.Json.JsonConvert.SerializeObject(model);
                return View("Index", model);
            }

            model.CurrentStep = CheckoutStep.PaymentMethod;
            return View("Index", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating the payment method");
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Step 5: Order confirmation
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PlaceOrder(CheckoutModel model)
    {
        try
        {
            var sessionId = HttpContext.Session.Id;
            model.Cart = await _cartService.GetCartAsync(sessionId);

            if (!ModelState.IsValid || !model.Cart.Items.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            // In a real implementation, the user ID should come from the authentication system
            Guid? userId = null; // User.IsAuthenticated ? GetCurrentUserId() : null;

            // Create the order
            var order = await _orderService.CreateOrderAsync(model, userId);

            if (order != null)
            {
                // Empty the cart once the order is confirmed
                await _cartService.ClearCartAsync(sessionId);

                // Redirect to the confirmation page
                return RedirectToAction(nameof(OrderConfirmation), new { orderId = order.OrderId });
            }

            ModelState.AddModelError("", "An error occurred while creating the order. Please try again.");
            return View("Index", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating the order");
            ModelState.AddModelError("", "An internal error occurred. Please try again.");
            return View("Index", model);
        }
    }

    /// <summary>
    /// Order confirmation page
    /// </summary>
    public async Task<IActionResult> OrderConfirmation(Guid orderId)
    {
        try
        {
            var order = await _orderService.GetOrderByIdAsync(orderId);

            if (order == null)
            {
                return NotFound();
            }

            var model = new OrderSummaryModel
            {
                Order = order,
                ConfirmationMessage = "Your order has been confirmed successfully!",
                NextSteps = new List<string>
                {
                    "You will receive a confirmation email within a few minutes",
                    "We will send you updates on the shipping status",
                    "You can track your order in the My Orders section"
                }
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error displaying the order confirmation");
            return RedirectToAction("Index", "Home");
        }
    }

    /// <summary>
    /// Applies a promo code
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ApplyPromoCode([FromBody] PromoCodeModel model)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(model.Code))
            {
                return Json(new { success = false, message = "Enter a valid promo code" });
            }

            var sessionId = HttpContext.Session.Id;
            var cart = await _cartService.GetCartAsync(sessionId);

            var isValid = await _orderService.ValidatePromoCodeAsync(model.Code, cart.Subtotal);

            if (isValid)
            {
                var discountAmount = await _orderService.CalculatePromoCodeDiscountAsync(model.Code, cart.Subtotal);

                return Json(new
                {
                    success = true,
                    discountAmount = discountAmount,
                    message = $"Promo code applied! Discount: €{discountAmount:F2}"
                });
            }

            return Json(new { success = false, message = "Invalid or expired promo code" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying the promo code");
            return Json(new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Removes a promo code
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> RemovePromoCode()
    {
        try
        {
            // Implementation for removing the promo code
            return Json(new { success = true, message = "Promo code removed" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing the promo code");
            return Json(new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Calculates shipping cost for an address
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CalculateShipping([FromBody] AddressModel address)
    {
        try
        {
            var sessionId = HttpContext.Session.Id;
            var cart = await _cartService.GetCartAsync(sessionId);

            var shippingOptions = await _orderService.GetShippingOptionsAsync(address, cart.Items);

            return Json(new
            {
                success = true,
                options = shippingOptions.Select(o => new
                {
                    id = o.ShippingOptionId,
                    name = o.Name,
                    description = o.Description,
                    cost = o.Cost,
                    deliveryDays = o.DeliveryDays,
                    estimatedDate = o.EstimatedDeliveryDate.ToString("dd/MM/yyyy")
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating shipping");
            return Json(new { success = false, message = "Error calculating shipping" });
        }
    }
}
