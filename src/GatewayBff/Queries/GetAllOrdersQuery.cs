namespace GatewayBff.Queries;

using MediatR;
using GatewayBff.Contracts;

public record GetAllOrdersQuery : IRequest<List<OrderDto>>;

public class GetAllOrdersQueryHandler : IRequestHandler<GetAllOrdersQuery, List<OrderDto>>
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GetAllOrdersQueryHandler> _logger;

    public GetAllOrdersQueryHandler(
        IHttpClientFactory httpClientFactory,
        ILogger<GetAllOrdersQueryHandler> logger)
    {
        _httpClient = httpClientFactory.CreateClient("OrderService");
        _logger = logger;
    }

    public async Task<List<OrderDto>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Recupero di tutti gli ordini");
        
        var response = await _httpClient.GetAsync("api/orders", cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>(cancellationToken);
        
        return orders ?? new List<OrderDto>();
    }
}
