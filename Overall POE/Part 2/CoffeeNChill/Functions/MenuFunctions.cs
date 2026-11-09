using Azure;
using Azure.Data.Tables;
using AzureTables;
using CoffeeNChill.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;


namespace CoffeeNChill.Functions;

public class MenuFunctions
{
    private readonly TableClient _table;

    public MenuFunctions()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        _table = new TableClient(
            connectionString,
            "MenuItems");

        _table.CreateIfNotExists();
    }

    // Creates a new menu item and adds it to the menu.
    [Function("CreateMenuItem")]
    public async Task<HttpResponseData> CreateMenuItem(
        [HttpTrigger(AuthorizationLevel.Function, "post",
            Route = "menu")] HttpRequestData req)
    {
        var item = await req.ReadFromJsonAsync<MenuItem>();

        var entity = new TableEntity(
            item.PartitionKey,
            item.RowKey)
        {
            ["Name"] = item.Name,
            ["Description"] = item.Description,
            ["Price"] = item.Price,
            ["IsAvailable"] = item.IsAvailable
        };

        await _table.AddEntityAsync(entity);

        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(item);

        return response;
    }

    // GET: /api/menu
    [Function("GetMenuItems")]
    public async Task<HttpResponseData> GetMenuItems(
        [HttpTrigger(AuthorizationLevel.Function, "get",
            Route = "menu")] HttpRequestData req)
    {
        var items = new List<MenuItem>();

        await foreach (var entity in _table.QueryAsync<TableEntity>())
        {
            items.Add(ToMenuItem(entity));
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(items);

        return response;
    }

    // GET: /api/menu/category/{category}
    [Function("GetMenuByCategory")]
    public async Task<HttpResponseData> GetMenuByCategory(
        [HttpTrigger(AuthorizationLevel.Function, "get",
            Route = "menu/category/{category}")]
            HttpRequestData req,
        string category)
    {
        var items = new List<MenuItem>();

        await foreach (var entity in _table.QueryAsync<TableEntity>(
            x => x.PartitionKey == category))
        {
            items.Add(ToMenuItem(entity));
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(items);

        return response;
    }

    // The "Update Menu Item" function/method. 
    [Function("UpdateMenuItem")]
    public async Task<HttpResponseData> UpdateMenuItem(
        [HttpTrigger(AuthorizationLevel.Function, "put",
            Route = "menu/{category}/{id}")]
            HttpRequestData req,
        string category,
        string id)
    {
        var item = await req.ReadFromJsonAsync<MenuItem>();

        var entity = new TableEntity(category, id)
        {
            ["Name"] = item.Name,
            ["Description"] = item.Description,
            ["Price"] = item.Price,
            ["IsAvailable"] = item.IsAvailable
        };

        await _table.UpsertEntityAsync(entity);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteAsJsonAsync(entity);

        return response;
    }

    // DELETE: /api/menu/{category}/{id}
    [Function("DeleteMenuItem")]
    public async Task<HttpResponseData> DeleteMenuItem(
        [HttpTrigger(AuthorizationLevel.Function, "delete",
            Route = "menu/{category}/{id}")]
            HttpRequestData req,
        string category,
        string id)
    {
        await _table.DeleteEntityAsync(category, id);

        var response = req.CreateResponse(HttpStatusCode.OK);
        await response.WriteStringAsync("Menu item deleted.");

        return response;
    }

    private static MenuItem ToMenuItem(TableEntity entity)
    {
        return new MenuItem
        {
            PartitionKey = entity.PartitionKey,
            RowKey = entity.RowKey,
            Name = entity.GetString("Name") ?? "",
            Description = entity.GetString("Description") ?? "",
            Price = entity.GetDouble("Price") ?? 0,
            IsAvailable = entity.GetBoolean("IsAvailable") ?? false
        };
    }
}
