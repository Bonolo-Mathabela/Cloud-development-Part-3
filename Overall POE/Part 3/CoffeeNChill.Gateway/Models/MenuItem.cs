namespace CoffeeNChill.Gateway.Models
{
    // The menu item as the Functions app sends and receives it.
    public class MenuItem
    {
        public string PartitionKey { get; set; } = "";
        public string RowKey { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public double Price { get; set; }
        public bool IsAvailable { get; set; }
    }
}
