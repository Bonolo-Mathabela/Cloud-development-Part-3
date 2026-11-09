namespace CoffeeNChill.Models
{
    // One order placed by a student. This is what gets put on the queue.
    internal class Order
    {
        public string OrderId { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public List<string> SelectedItemSKUs { get; set; } = new();
        public double TotalPrice { get; set; }
        public DateTime OrderTimestamp { get; set; }
    }
}
