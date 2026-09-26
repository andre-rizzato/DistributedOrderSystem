namespace GatewayBff.Commands;

using GatewayBff.Clients;
using MediatR;

public record AdjustInventoryCommand(Guid ProductId, int Delta) : IRequest<bool>;

public class AdjustInventoryCommandHandler : IRequestHandler<AdjustInventoryCommand, bool>
{
    private readonly IInventoryServiceClient _inventory;

    public AdjustInventoryCommandHandler(IInventoryServiceClient inventory)
    {
        _inventory = inventory;
    }

    public Task<bool> Handle(AdjustInventoryCommand request, CancellationToken ct) =>
        _inventory.AdjustInventoryAsync(request.ProductId, request.Delta, ct);
}
