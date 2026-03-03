namespace OrderService.Controllers;

using Microsoft.AspNetCore.Mvc;
using OrderService.Application.Services;
using OrderService.Domain.Aggregates;
using OrderService.Domain.Exceptions;

// ── DTOs per mantenere il contratto API identico ──
public record OrderDto(int Id, DateTime CreatedAt, string Status, decimal Total, List<OrderItemResponseDto> Items);
public record OrderItemResponseDto(int Id, int OrderId, Guid ProductId, int Quantity, decimal UnitPrice);

/// <summary>
/// Controller thin per le query sugli ordini (read-side).
/// Delega interamente all'Application Service — nessuna logica di business qui.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderApplicationService _appService;

    public OrdersController(IOrderApplicationService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderDto>>> GetAllOrders(CancellationToken ct)
    {
        var orders = await _appService.GetAllOrdersAsync(ct);
        return Ok(orders.Select(ToDto).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int id, CancellationToken ct)
    {
        var order = await _appService.GetOrderByIdAsync(id, ct);
        if (order is null)
            return NotFound($"Order {id} not found");
        return Ok(ToDto(order));
    }

    private static OrderDto ToDto(Order o) => new(
        o.Id, o.CreatedAt, o.Status.Value, o.Total.Amount,
        o.Items.Select(i => new OrderItemResponseDto(
            i.Id, i.OrderId, i.ProductId, i.Quantity, i.UnitPrice.Amount)).ToList());
}

/// <summary>
/// Controller thin per i comandi sugli ordini (write-side).
/// L'orchestrazione (persistenza + evento Kafka) è nell'Application Service.
/// Le regole di business (invarianti, transizioni di stato) sono nel Domain layer.
/// </summary>
[ApiController]
[Route("api/commands")]
public class OrderCommandsController : ControllerBase
{
    private readonly IOrderApplicationService _appService;

    public OrderCommandsController(IOrderApplicationService appService)
    {
        _appService = appService;
    }

    public record CreateOrderRequest(List<OrderItemDto> Items);
    public record OrderItemDto(Guid ProductId, int Quantity, decimal UnitPrice);
    public record CreateOrderResponse(int OrderId, string Status, decimal Total);

    [HttpPost("orders")]
    public async Task<ActionResult<CreateOrderResponse>> CreateOrder(
        [FromBody] CreateOrderRequest request,
        CancellationToken ct)
    {
        if (request?.Items is null || request.Items.Count == 0)
            return BadRequest("At least one item is required");

        try
        {
            var items = request.Items.Select(i => (i.ProductId, i.Quantity, i.UnitPrice));
            var created = await _appService.CreateOrderAsync(items, ct);

            return CreatedAtAction(
                nameof(OrdersController.GetOrder),
                "Orders",
                new { id = created.Id },
                new CreateOrderResponse(created.Id, created.Status.Value, created.Total.Amount));
        }
        catch (OrderDomainException ex)
        {
            return BadRequest(ex.Message);
        }
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

        try
        {
            var success = await _appService.UpdateOrderStatusAsync(id, request.Status, ct);
            if (!success)
                return NotFound($"Order {id} not found");
            return NoContent();
        }
        catch (OrderDomainException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
