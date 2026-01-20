namespace CustomerService.Storage;

using CustomerService.Models;

public interface ICartStorage
{
    Task<Cart> GetCartAsync(string sessionId, CancellationToken ct = default);
    Task SaveCartAsync(Cart cart, CancellationToken ct = default);
    Task DeleteCartAsync(string sessionId, CancellationToken ct = default);
}
