namespace InventoryService.Controllers;
using InventoryService.Models;
using InventoryService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryWorkerService _inventoryWorkerService;

    public InventoryController(IInventoryWorkerService inventoryWorkerService)
    {
        _inventoryWorkerService = inventoryWorkerService;
    }

    [HttpGet("{productId:int}")]
    public async Task<ActionResult<InventoryItem>> Get(int productId, CancellationToken ct)
    {
        var item = await _inventoryWorkerService.GetInventoryByProductIdAsync(productId, ct);
        if (item is null) return NotFound();
        return Ok(item);
    }
    public record SeedItem(int ProductId, int Quantity);

    [HttpPost("seed")]
    public async Task<IActionResult> Seed(List<SeedItem> items, CancellationToken ct)
    {
        foreach (var i in items)
        {
            await _inventoryWorkerService.SetInventoryQuantityAsync(i.ProductId, i.Quantity, ct);
        }

        return Ok();
    }

    public record AdjustRequest(int ProductId, int Delta);

    [HttpPost("adjust")]
    public async Task<IActionResult> Adjust(AdjustRequest request, CancellationToken ct)
    {
        var success = await _inventoryWorkerService.AdjustInventoryQuantityAsync(request.ProductId, request.Delta, ct);
        if (!success) return BadRequest("Not enough inventory or product not found");
        return Ok();
    }
}   