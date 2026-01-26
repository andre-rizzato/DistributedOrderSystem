namespace OrderService.Controllers;

using Microsoft.AspNetCore.Mvc;
using OrderService.Models;
using OrderService.Services;
using OrderService.Messaging;
using Shared.Messages;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<Order>>> GetAllOrders(CancellationToken ct)
    {
        var orders = await _orderService.GetAllOrdersAsync(ct);
        return Ok(orders);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Order>> GetOrder(int id, CancellationToken ct)
    {
        var order = await _orderService.GetOrderByIdAsync(id, ct);
        if (order == null)
            return NotFound($"Order {id} not found");
        
        return Ok(order);
    }
}

[ApiController]
[Route("api/commands")]
public class OrderCommandsController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IOrderEventProducer _eventProducer;
    private readonly ILogger<OrderCommandsController> _logger;

    public OrderCommandsController(
        IOrderService orderService, 
        IOrderEventProducer eventProducer,
        ILogger<OrderCommandsController> logger)
    {
        _orderService = orderService;
        _eventProducer = eventProducer;
        _logger = logger;
    }

    public record CreateOrderRequest(List<OrderItemDto> Items);
    public record OrderItemDto(Guid ProductId, int Quantity, decimal UnitPrice);
    public record CreateOrderResponse(int OrderId, string Status, decimal Total);

    [HttpPost("orders")]
    public async Task<ActionResult<CreateOrderResponse>> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken ct)
    {
        if (request?.Items == null || request.Items.Count == 0)
            return BadRequest("At least one item is required");

        var order = new Order
        {
            Items = request.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };

        order.Total = order.Items.Sum(i => i.Quantity * i.UnitPrice);

        var created = await _orderService.CreateOrderAsync(order, ct);

        // Pubblica evento su Kafka per aggiornamento inventario asincrono
        try
        {
            var orderEvent = new OrderCreatedEvent
            {
                OrderId = created.Id.ToString(),
                CreatedAt = created.CreatedAt,
                Items = created.Items.Select(i => new OrderItemEvent
                {
                    ProductId = i.ProductId.ToString(),
                    Quantity = i.Quantity
                }).ToList()
            };

            await _eventProducer.PublishOrderCreatedAsync(orderEvent, ct);
            _logger.LogInformation("Pubblicato evento OrderCreated per Ordine {OrderId}", created.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Impossibile pubblicare evento OrderCreated per Ordine {OrderId}", created.Id);
            // Non fallire la richiesta se la pubblicazione dell'evento fallisce
        }

        return CreatedAtAction(
            nameof(OrdersController.GetOrder),
            "Orders",
            new { id = created.Id },
            new CreateOrderResponse(created.Id, created.Status, created.Total)
        );
    }

    public record UpdateOrderStatusRequest(string Status);

    [HttpPut("orders/{id:int}/status")]
    public async Task<ActionResult> UpdateOrderStatus(
        int id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request?.Status))
            return BadRequest("Status is required");

        var success = await _orderService.UpdateOrderStatusAsync(id, request.Status, ct);
        if (!success)
            return NotFound($"Order {id} not found");

        return NoContent();
    }
}
