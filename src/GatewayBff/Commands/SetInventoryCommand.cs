namespace GatewayBff.Commands;

using GatewayBff.Clients;
using MediatR;

public record SetInventoryCommand(Guid ProductId, int Quantity) : IRequest<bool>;

public class SetInventoryCommandHandler : IRequestHandler<SetInventoryCommand, bool>
{
    private readonly IInventoryServiceClient _inventory;

    public SetInventoryCommandHandler(IInventoryServiceClient inventory)
    {
        _inventory = inventory;
    }

    public Task<bool> Handle(SetInventoryCommand request, CancellationToken ct) =>
        _inventory.SetInventoryAsync(request.ProductId, request.Quantity, ct);
}
