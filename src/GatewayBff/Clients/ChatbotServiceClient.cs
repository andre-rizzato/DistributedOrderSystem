namespace GatewayBff.Clients;

using System.Net.Http.Json;

public class ChatbotServiceClient : IChatbotServiceClient
{
    private readonly HttpClient _http;

    public ChatbotServiceClient(HttpClient http)
    {
        _http = http;
    }

    public Task<HttpResponseMessage> GetHealthAsync(CancellationToken ct) =>
        _http.GetAsync("/api/chatwidget/health", ct);

    public Task<HttpResponseMessage> SendMessageAsync(object request, CancellationToken ct) =>
        _http.PostAsJsonAsync("/api/chat/message", request, ct);
}
