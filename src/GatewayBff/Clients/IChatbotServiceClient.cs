namespace GatewayBff.Clients;

public interface IChatbotServiceClient
{
    Task<HttpResponseMessage> GetHealthAsync(CancellationToken ct);
    Task<HttpResponseMessage> SendMessageAsync(object request, CancellationToken ct);
}
