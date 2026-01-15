using Microsoft.AspNetCore.Mvc;
using CustomerWebsite.Models;
using CustomerWebsite.Services;

namespace CustomerWebsite.Controllers;

/// <summary>
/// Controller per il processo di checkout
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
    /// Pagina principale del checkout
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            var sessionId = HttpContext.Session.Id;
            var cart = await _cartService.GetCartAsync(sessionId);

            if (!cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Il tuo carrello è vuoto.";
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
            _logger.LogError(ex, "Errore durante l'inizializzazione del checkout");
            return RedirectToAction("Index", "Cart");
        }
    }

    /// <summary>
    /// Step 2: Indirizzo di spedizione
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
                // Carica le opzioni di spedizione disponibili
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
            _logger.LogError(ex, "Errore durante la validazione dell'indirizzo di spedizione");
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Step 3: Metodo di spedizione
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ShippingMethod(CheckoutModel model, Guid shippingOptionId)
    {
        try
        {
            var sessionId = HttpContext.Session.Id;
            model.Cart = await _cartService.GetCartAsync(sessionId);

            // Trova l'opzione di spedizione selezionata
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

            ModelState.AddModelError("", "Seleziona un metodo di spedizione valido.");
            model.CurrentStep = CheckoutStep.ShippingMethod;
            return View("Index", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la selezione del metodo di spedizione");
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Step 4: Metodo di pagamento
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
                // Calcola le tasse finali
                model.TaxAmount = model.Cart.Subtotal * 0.22m; // IVA italiana 22%

                model.CurrentStep = CheckoutStep.ReviewOrder;

                TempData["CheckoutModel"] = Newtonsoft.Json.JsonConvert.SerializeObject(model);
                return View("Index", model);
            }

            model.CurrentStep = CheckoutStep.PaymentMethod;
            return View("Index", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la validazione del metodo di pagamento");
            return RedirectToAction(nameof(Index));
        }
    }

    /// <summary>
    /// Step 5: Conferma ordine
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

            // In un'implementazione reale, dovresti ottenere l'ID utente dal sistema di autenticazione
            Guid? userId = null; // User.IsAuthenticated ? GetCurrentUserId() : null;

            // Crea l'ordine
            var order = await _orderService.CreateOrderAsync(model, userId);

            if (order != null)
            {
                // Svuota il carrello dopo l'ordine confermato
                await _cartService.ClearCartAsync(sessionId);

                // Reindirizza alla pagina di conferma
                return RedirectToAction(nameof(OrderConfirmation), new { orderId = order.OrderId });
            }

            ModelState.AddModelError("", "Si è verificato un errore durante la creazione dell'ordine. Riprova.");
            return View("Index", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la creazione dell'ordine");
            ModelState.AddModelError("", "Si è verificato un errore interno. Riprova.");
            return View("Index", model);
        }
    }

    /// <summary>
    /// Pagina di conferma ordine
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
                ConfirmationMessage = "Il tuo ordine è stato confermato con successo!",
                NextSteps = new List<string>
                {
                    "Riceverai una email di conferma entro pochi minuti",
                    "Ti invieremo aggiornamenti sullo stato della spedizione",
                    "Puoi tracciare il tuo ordine nella sezione I miei ordini"
                }
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la visualizzazione della conferma ordine");
            return RedirectToAction("Index", "Home");
        }
    }

    /// <summary>
    /// Applica codice promozionale
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ApplyPromoCode([FromBody] PromoCodeModel model)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(model.Code))
            {
                return Json(new { success = false, message = "Inserisci un codice promozionale valido" });
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
                    message = $"Codice promozionale applicato! Sconto: €{discountAmount:F2}"
                });
            }

            return Json(new { success = false, message = "Codice promozionale non valido o scaduto" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'applicazione del codice promozionale");
            return Json(new { success = false, message = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Rimuove codice promozionale
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> RemovePromoCode()
    {
        try
        {
            // Implementazione per rimuovere il codice promozionale
            return Json(new { success = true, message = "Codice promozionale rimosso" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la rimozione del codice promozionale");
            return Json(new { success = false, message = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Calcola le spese di spedizione per un indirizzo
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
            _logger.LogError(ex, "Errore durante il calcolo della spedizione");
            return Json(new { success = false, message = "Errore nel calcolo della spedizione" });
        }
    }
}