namespace CoffeeNChill.Gateway.DTOs
{
    /// <summary>A simple success message.</summary>
    public class MessageResponseDto
    {
        /// <example>Done.</example>
        public string Message { get; set; } = "";
    }

    /// <summary>The body of every error sent by the gateway.</summary>
    public class ErrorResponseDto
    {
        /// <example>400</example>
        public int StatusCode { get; set; }

        /// <example>CustomerName is required.</example>
        public string Message { get; set; } = "";
    }
}
