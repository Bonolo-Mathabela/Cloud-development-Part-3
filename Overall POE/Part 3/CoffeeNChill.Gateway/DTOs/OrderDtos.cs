using System.ComponentModel.DataAnnotations;

namespace CoffeeNChill.Gateway.DTOs
{
    /// <summary>An order placed by a student.</summary>
    public class OrderRequestDto
    {
        /// <summary>Optional. Generated if it is left out.</summary>
        /// <example>ORD-2026-8801</example>
        [RegularExpression(@"^[A-Za-z0-9\-]+$", ErrorMessage = "OrderId can only have letters, numbers and dashes.")]
        public string? OrderId { get; set; }

        /// <example>Jane Smith</example>
        [Required(ErrorMessage = "CustomerName is required.")]
        public string CustomerName { get; set; } = "";

        /// <summary>The SKUs of the items that were ordered.</summary>
        /// <example>["COF-001", "PAS-104"]</example>
        [Required(ErrorMessage = "SelectedItemSKUs is required.")]
        [MinLength(1, ErrorMessage = "SelectedItemSKUs must have at least one item.")]
        public List<string> SelectedItemSKUs { get; set; } = new();

        /// <example>65.00</example>
        [Range(0.01, 100000, ErrorMessage = "TotalPrice must be more than 0.")]
        public double TotalPrice { get; set; }

        /// <summary>Optional. Generated if it is left out.</summary>
        public DateTime? OrderTimestamp { get; set; }
    }

    /// <summary>An order that is on the queue.</summary>
    public class QueuedOrderDto
    {
        /// <example>ORD-2026-8801</example>
        public string OrderId { get; set; } = "";

        /// <example>Jane Smith</example>
        public string CustomerName { get; set; } = "";

        /// <example>["COF-001", "PAS-104"]</example>
        public List<string> SelectedItemSKUs { get; set; } = new();

        /// <example>65.00</example>
        public double TotalPrice { get; set; }

        public DateTime OrderTimestamp { get; set; }
    }

    /// <summary>Returned when an order has been accepted.</summary>
    public class OrderQueuedResponseDto
    {
        /// <example>Order received and queued.</example>
        public string Message { get; set; } = "";

        public QueuedOrderDto? Order { get; set; }
    }

    /// <summary>An order in the Orders table and its current status.</summary>
    public class OrderStatusResponseDto
    {
        /// <summary>The date of the order. It is the PartitionKey.</summary>
        /// <example>2026-10-12</example>
        public string OrderDate { get; set; } = "";

        /// <example>ORD-2026-8801</example>
        public string OrderId { get; set; } = "";

        /// <example>Jane Smith</example>
        public string CustomerName { get; set; } = "";

        /// <summary>The SKUs separated by commas.</summary>
        /// <example>COF-001,PAS-104</example>
        public string SelectedItemSKUs { get; set; } = "";

        /// <example>65.00</example>
        public double TotalPrice { get; set; }

        public DateTime? OrderTimestamp { get; set; }

        /// <example>Preparing</example>
        public string Status { get; set; } = "";

        public DateTimeOffset? LastUpdated { get; set; }
    }

    /// <summary>A new status for an order.</summary>
    public class UpdateOrderStatusDto
    {
        /// <example>ORD-2026-8801</example>
        [Required(ErrorMessage = "OrderId is required.")]
        public string OrderId { get; set; } = "";

        /// <summary>Received, Preparing, Ready or Collected.</summary>
        /// <example>Ready</example>
        [Required(ErrorMessage = "Status is required.")]
        [RegularExpression("^(Received|Preparing|Ready|Collected)$",
            ErrorMessage = "Status must be Received, Preparing, Ready or Collected.")]
        public string Status { get; set; } = "";
    }
}
