using Azure;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using CoffeeNChill.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CoffeeNChill.Functions;

public class OrderFunctions
{
    public const string QueueName = "order-processing-queue";
    public const string TableName = "Orders";

    private readonly QueueClient _queue;
    private readonly TableClient _table;
    private readonly ILogger<OrderFunctions> _logger;

    public OrderFunctions(ILogger<OrderFunctions> logger)
    {
        _logger = logger;

        var connectionString =
            Environment.GetEnvironmentVariable("AzureWebJobsStorage");

       
        _queue = new QueueClient(connectionString, QueueName);

        _table = new TableClient(connectionString, TableName);
    }

    // POST: /api/orders/queue
    // Takes the order, checks it, turns it into JSON and puts it on the queue.
    [Function("QueueOrder")]
    public async Task<HttpResponseData> QueueOrder(
        [HttpTrigger(AuthorizationLevel.Function, "post",
            Route = "orders/queue")] HttpRequestData req)
    {
        Order? order;

        try
        {
            order = await req.ReadFromJsonAsync<Order>();
        }
        catch (JsonException)
        {
            return await BadRequest(req, "The body is not valid JSON.");
        }

        if (order == null)
        {
            return await BadRequest(req, "The body is empty.");
        }

        // Validation of the order fields
        if (string.IsNullOrWhiteSpace(order.CustomerName))
        {
            return await BadRequest(req, "CustomerName is required.");
        }

        if (order.SelectedItemSKUs == null || order.SelectedItemSKUs.Count == 0)
        {
            return await BadRequest(req, "SelectedItemSKUs must have at least one item.");
        }

        if (order.TotalPrice <= 0)
        {
            return await BadRequest(req, "TotalPrice must be more than 0.");
        }

        
        if (string.IsNullOrWhiteSpace(order.OrderId))
        {
            order.OrderId = "ORD-" + DateTime.UtcNow.Year + "-"
                + Random.Shared.Next(1000, 9999);
        }

        if (order.OrderTimestamp == default)
        {
            order.OrderTimestamp = DateTime.UtcNow;
        }

        // 1. Serialize the order into a JSON payload
        string json = JsonSerializer.Serialize(order);

        
        string message = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        _logger.LogInformation("Order {OrderId} JSON: {Json}", order.OrderId, json);
        _logger.LogInformation("Order {OrderId} Base64: {Message}", order.OrderId, message);

        try
        {
            await _queue.CreateIfNotExistsAsync();
            await _queue.SendMessageAsync(message);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Could not send order {OrderId} to the queue.", order.OrderId);

            var error = req.CreateResponse(HttpStatusCode.ServiceUnavailable);
            await error.WriteStringAsync("The order queue is not available. Try again later.");
            return error;
        }

        _logger.LogInformation("Order {OrderId} was placed on {Queue}.", order.OrderId, QueueName);

        var response = req.CreateResponse(HttpStatusCode.Accepted);
        await response.WriteAsJsonAsync(new
        {
            Message = "Order received and queued.",
            Order = order
        });

        return response;
    }

    // GET: /api/orders
    // Lists every order in the Orders table with its current status.
    [Function("GetOrders")]
    public async Task<HttpResponseData> GetOrders(
        [HttpTrigger(AuthorizationLevel.Function, "get",
            Route = "orders")] HttpRequestData req)
    {
        await _table.CreateIfNotExistsAsync();

        var orders = new List<object>();

        await foreach (var entity in _table.QueryAsync<TableEntity>())
        {
            orders.Add(ToOrderStatus(entity));
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(orders);

        return response;
    }

    // GET: /api/orders/{orderId}
    // Used to watch the status change: Received -> Preparing -> Ready -> Collected
    [Function("GetOrderStatus")]
    public async Task<HttpResponseData> GetOrderStatus(
        [HttpTrigger(AuthorizationLevel.Function, "get",
            Route = "orders/{orderId}")] HttpRequestData req,
        string orderId)
    {
        await _table.CreateIfNotExistsAsync();

       
        await foreach (var entity in _table.QueryAsync<TableEntity>(
            x => x.RowKey == orderId))
        {
            var found = req.CreateResponse(HttpStatusCode.OK);
            await found.WriteAsJsonAsync(ToOrderStatus(entity));
            return found;
        }

        var notFound = req.CreateResponse(HttpStatusCode.NotFound);
        await notFound.WriteStringAsync(
            $"Order '{orderId}' is not in the Orders table yet.");

        return notFound;
    }

    private static object ToOrderStatus(TableEntity entity)
    {
        return new
        {
            OrderDate = entity.PartitionKey,
            OrderId = entity.RowKey,
            CustomerName = entity.GetString("CustomerName") ?? "",
            SelectedItemSKUs = entity.GetString("SelectedItemSKUs") ?? "",
            TotalPrice = entity.GetDouble("TotalPrice") ?? 0,
            OrderTimestamp = entity.GetDateTime("OrderTimestamp"),
            Status = entity.GetString("Status") ?? "",
            LastUpdated = entity.Timestamp
        };
    }

    private static async Task<HttpResponseData> BadRequest(
        HttpRequestData req, string reason)
    {
        var response = req.CreateResponse(HttpStatusCode.BadRequest);
        await response.WriteStringAsync(reason);
        return response;
    }
}
