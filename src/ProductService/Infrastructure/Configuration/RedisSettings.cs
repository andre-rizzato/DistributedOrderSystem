namespace ProductService.Infrastructure.Configuration;

/// <summary>
/// Configurazione tipizzata per Redis.
/// </summary>
public class RedisSettings
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public string Prefix { get; set; } = "product:";
}
