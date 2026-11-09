using Azure;
using CoffeeNChill.Gateway.DTOs;
using CoffeeNChill.Gateway.Services;

namespace CoffeeNChill.Gateway.Middleware
{
    // Catches every error that is not handled in a controller and sends
    // back the same JSON body each time, without showing internal details.
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Request failed: {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                // A file download may already be busy, so nothing more can be written.
                if (context.Response.HasStarted)
                {
                    throw;
                }

                var (statusCode, message) = GetError(ex);

                context.Response.Clear();
                context.Response.StatusCode = statusCode;

                await context.Response.WriteAsJsonAsync(new ErrorResponseDto
                {
                    StatusCode = statusCode,
                    Message = message
                });
            }
        }

        public static (int StatusCode, string Message) GetError(Exception ex)
        {
            switch (ex)
            {
                // These are problems with the request, so the client can be told.
                case FunctionsException functions when
                    functions.StatusCode is 400 or 404 or 409:
                    return (functions.StatusCode, functions.Message);

                // Anything else from the Functions app is a problem on our side.
                case FunctionsException:
                    return (StatusCodes.Status502BadGateway, "The backend service returned an error.");

                case HttpRequestException:
                case TaskCanceledException:
                    return (StatusCodes.Status503ServiceUnavailable, "The backend service is not available. Try again later.");

                case RequestFailedException:
                    return (StatusCodes.Status503ServiceUnavailable, "Storage is not available. Try again later.");

                default:
                    return (StatusCodes.Status500InternalServerError, "Something went wrong on the server.");
            }
        }
    }
}
