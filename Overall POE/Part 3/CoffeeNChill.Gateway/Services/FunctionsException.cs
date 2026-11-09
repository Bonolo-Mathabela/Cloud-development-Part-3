namespace CoffeeNChill.Gateway.Services
{
    // Thrown when the Functions app answers with an error status code.
    // The exception middleware turns it into the response for the client.
    public class FunctionsException : Exception
    {
        public int StatusCode { get; }

        public FunctionsException(int statusCode, string message) : base(message)
        {
            StatusCode = statusCode;
        }
    }
}
