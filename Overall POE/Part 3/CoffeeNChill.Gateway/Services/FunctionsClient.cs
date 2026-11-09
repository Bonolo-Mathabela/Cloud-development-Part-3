using CoffeeNChill.Gateway.DTOs;
using CoffeeNChill.Gateway.Models;

namespace CoffeeNChill.Gateway.Services
{
    // All the calls from the gateway to the Azure Functions app are in this class.
    // The base address and x-functions-key header are set in Program.cs.
    public class FunctionsClient
    {
        private readonly HttpClient _http;

        public FunctionsClient(HttpClient http)
        {
            _http = http;
        }

        // MENU

        public async Task<List<MenuItem>> GetMenuAsync()
        {
            var response = await _http.GetAsync("api/menu");
            await CheckResponse(response);

            return await response.Content.ReadFromJsonAsync<List<MenuItem>>() ?? new List<MenuItem>();
        }

        public async Task<List<MenuItem>> GetMenuByCategoryAsync(string category)
        {
            var response = await _http.GetAsync($"api/menu/category/{Uri.EscapeDataString(category)}");
            await CheckResponse(response);

            return await response.Content.ReadFromJsonAsync<List<MenuItem>>() ?? new List<MenuItem>();
        }

        public async Task CreateMenuItemAsync(MenuItem item)
        {
            var response = await _http.PostAsJsonAsync("api/menu", item);
            await CheckResponse(response);
        }

        public async Task UpdateMenuItemAsync(string category, string id, MenuItem item)
        {
            var response = await _http.PutAsJsonAsync(
                $"api/menu/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(id)}", item);
            await CheckResponse(response);
        }

        public async Task DeleteMenuItemAsync(string category, string id)
        {
            var response = await _http.DeleteAsync(
                $"api/menu/{Uri.EscapeDataString(category)}/{Uri.EscapeDataString(id)}");
            await CheckResponse(response);
        }

        // ORDERS

        public async Task<OrderQueuedResponseDto> QueueOrderAsync(Order order)
        {
            var response = await _http.PostAsJsonAsync("api/orders/queue", order);
            await CheckResponse(response);

            return await response.Content.ReadFromJsonAsync<OrderQueuedResponseDto>()
                ?? new OrderQueuedResponseDto();
        }

        public async Task<List<OrderStatusResponseDto>> GetOrdersAsync()
        {
            var response = await _http.GetAsync("api/orders");
            await CheckResponse(response);

            return await response.Content.ReadFromJsonAsync<List<OrderStatusResponseDto>>()
                ?? new List<OrderStatusResponseDto>();
        }

        public async Task<OrderStatusResponseDto> GetOrderAsync(string orderId)
        {
            var response = await _http.GetAsync($"api/orders/{Uri.EscapeDataString(orderId)}");
            await CheckResponse(response);

            return await response.Content.ReadFromJsonAsync<OrderStatusResponseDto>()
                ?? new OrderStatusResponseDto();
        }

        public async Task<string> UpdateOrderStatusAsync(UpdateOrderStatusDto update)
        {
            var response = await _http.PutAsJsonAsync("api/orders/status", update);
            await CheckResponse(response);

            return await response.Content.ReadAsStringAsync();
        }

        // DOCUMENTS

        public async Task UploadDocumentAsync(string fileName, Stream file)
        {
            var response = await _http.PostAsync(
                $"api/documents/upload?fileName={Uri.EscapeDataString(fileName)}",
                new StreamContent(file));
            await CheckResponse(response);
        }

        public async Task<List<string>> GetDocumentsAsync()
        {
            var response = await _http.GetAsync("api/documents");
            await CheckResponse(response);

            return await response.Content.ReadFromJsonAsync<List<string>>() ?? new List<string>();
        }

        public async Task<Stream> DownloadDocumentAsync(string fileName)
        {
            var response = await _http.GetAsync(
                $"api/documents/download/{Uri.EscapeDataString(fileName)}",
                HttpCompletionOption.ResponseHeadersRead);
            await CheckResponse(response);

            return await response.Content.ReadAsStreamAsync();
        }

        public async Task DeleteDocumentAsync(string fileName)
        {
            var response = await _http.DeleteAsync($"api/documents/{Uri.EscapeDataString(fileName)}");
            await CheckResponse(response);
        }

        // If the Functions app sent an error, throw it so the middleware can handle it.
        private static async Task CheckResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                string message = await response.Content.ReadAsStringAsync();
                throw new FunctionsException((int)response.StatusCode, message);
            }
        }
    }
}
