namespace GatewayBff.Queries;

using MediatR;
using GatewayBff.Contracts;

public record GetOrderByIdQuery(int OrderId) : IRequest<OrderDto?>;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GetOrderByIdQueryHandler> _logger;

    public GetOrderByIdQueryHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<GetOrderByIdQueryHandler> logger)
    {
        _httpClient = httpClientFactory.CreateClient("OrderService");
        _logger = logger;
    }

    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Recupero ordine {OrderId}", request.OrderId);
        
        var response = await _httpClient.GetAsync($"api/orders/{request.OrderId}", cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Ordine {OrderId} non trovato", request.OrderId);
            return null;
        }
        
        var order = await response.Content.ReadFromJsonAsync<OrderDto>(cancellationToken);
        
        return order;
    }
}
