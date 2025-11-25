namespace InventoryService.Configuration;

public class RedisSettings
{
  public string ConnectionString { get; set; } = "localhost:6379";
  public string InventoryPrefix  { get; set;} = "Inventory:";
}