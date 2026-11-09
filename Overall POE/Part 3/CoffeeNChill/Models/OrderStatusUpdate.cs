namespace CoffeeNChill.Models
{
    // The body of PUT /api/orders/status.
    internal class OrderStatusUpdate
    {
        public string OrderId { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
