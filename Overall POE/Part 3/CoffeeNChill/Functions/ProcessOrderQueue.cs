using Azure.Data.Tables;
using CoffeeNChill.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace CoffeeNChill.Functions;

public class ProcessOrderQueue
{
    private readonly TableClient _table;
    private readonly ILogger<ProcessOrderQueue> _logger;

    // Order stages, 30 seconds apart
    private static readonly string[] Steps = { "Preparing", "Ready", "Collected" };
    private static readonly TimeSpan StepDelay = TimeSpan.FromSeconds(30);

    public ProcessOrderQueue(ILogger<ProcessOrderQueue> logger)
    {
        _logger = logger;

        var connectionString =
            Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        _table = new TableClient(connectionString, OrderFunctions.TableName);
        _table.CreateIfNotExists();
    }

    // Triggered when a message lands on order-processing- queue 
    [Function("ProcessOrderQueue")]
    public async Task Run(
        [QueueTrigger(OrderFunctions.QueueName, Connection = "AzureWebJobsStorage")]
        string queueMessage,
        FunctionContext context)
    {
        
        // After 5 failed tries the message goes to the poison queue.
        context.BindingContext.BindingData.TryGetValue("DequeueCount", out var dequeueCount);

        _logger.LogInformation("Queue message received (attempt {Attempt}): {Message}",
            dequeueCount, queueMessage);

        Order? order;

        try
        {
            // 1. Base64 decode the message back into the JSON text
            var jsonBytes = Convert.FromBase64String(queueMessage);
            var json = Encoding.UTF8.GetString(jsonBytes);

            _logger.LogInformation("Decoded JSON: {Json}", json);

            // JSON to Order object
            order = JsonSerializer.Deserialize<Order>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (FormatException ex)
        {
            // Throwing makes the host retry, and eventually poison the message.
            _logger.LogError(ex, "Queue message is not valid Base64: {Message}", queueMessage);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Queue message is not valid JSON: {Message}", queueMessage);
            throw;
        }

        if (order == null || string.IsNullOrWhiteSpace(order.OrderId))
        {
            _logger.LogError("Queue message has no OrderId: {Message}", queueMessage);
            throw new InvalidOperationException("Queue message has no OrderId.");
        }

       
        string orderDate = order.OrderTimestamp.ToString("yyyy-MM-dd");

        var entity = new TableEntity(orderDate, order.OrderId)
        {
            ["CustomerName"] = order.CustomerName,
            ["SelectedItemSKUs"] = string.Join(",", order.SelectedItemSKUs),
            ["TotalPrice"] = order.TotalPrice,
            ["OrderTimestamp"] = DateTime.SpecifyKind(order.OrderTimestamp, DateTimeKind.Utc),
            ["Status"] = "Received"
        };

        await _table.UpsertEntityAsync(entity);

        _logger.LogInformation("Order {OrderId}: Received", order.OrderId);

        // Received -> Preparing -> Ready -> Collected
        foreach (var step in Steps)
        {
            _logger.LogInformation("Order {OrderId}: Waiting for processing...", order.OrderId);

            await Task.Delay(StepDelay);

            var update = new TableEntity(orderDate, order.OrderId)
            {
                ["Status"] = step
            };

            
            await _table.UpsertEntityAsync(update, TableUpdateMode.Merge);

            _logger.LogInformation("Order {OrderId}: {Status}", order.OrderId, step);
        }
    }

    // Messages that failed 5 times end up here. We log them so nothing is lost silently.
    [Function("ProcessOrderQueuePoison")]
    public void RunPoison(
        [QueueTrigger(OrderFunctions.QueueName + "-poison", Connection = "AzureWebJobsStorage")]
        string queueMessage)
    {
        _logger.LogError("POISON MESSAGE - order could not be processed after 5 attempts: {Message}",
            queueMessage);

        
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(queueMessage));
            _logger.LogError("POISON MESSAGE decoded: {Json}", json);
        }
        catch (FormatException)
        {
            _logger.LogError("POISON MESSAGE was not valid Base64.");
        }
    }
}
