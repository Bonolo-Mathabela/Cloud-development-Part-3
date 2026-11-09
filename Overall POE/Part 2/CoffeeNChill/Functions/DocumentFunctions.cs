using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace CoffeeNChill.Functions
{
    public class DocumentFunctions
    {
        private readonly BlobContainerClient _container;

        public DocumentFunctions()
        {
            var client = new BlobServiceClient(
                Environment.GetEnvironmentVariable("AzureWebJobsStorage"));

            _container = client.GetBlobContainerClient("staff-docs");
            _container.CreateIfNotExists();
        }

        // POST: /api/documents/upload
        [Function("UploadStaffDocument")]
        public async Task<HttpResponseData> UploadStaffDocument(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post",
            Route = "documents/upload")] HttpRequestData req)
        {
            string? fileName = req.Query["fileName"];

            if (string.IsNullOrWhiteSpace(fileName))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync(
                    "Add a fileName to the query string.");
                return badRequest;
            }

            var blob = _container.GetBlobClient(fileName);

            await blob.UploadAsync(req.Body, overwrite: true);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"Uploaded: {fileName}");

            return response;
        }

        // GET: /api/documents
        [Function("ListStaffDocuments")]
        public async Task<HttpResponseData> ListStaffDocuments(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get",
            Route = "documents")] HttpRequestData req)
        {
            var files = new List<string>();

            await foreach (var blob in _container.GetBlobsAsync())
            {
                files.Add(blob.Name);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(files);

            return response;
        }

        // GET: /api/documents/download/{fileName}
        [Function("DownloadStaffDocument")]
        public async Task<HttpResponseData> DownloadStaffDocument(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get",
            Route = "documents/download/{fileName}")]
            HttpRequestData req,
            string fileName)
        {
            var blob = _container.GetBlobClient(fileName);

            if (!await blob.ExistsAsync())
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync(
                    $"File '{fileName}' was not found.");
                return notFound;
            }

            var download = await blob.DownloadAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);

            await download.Value.Content.CopyToAsync(response.Body);

            return response;
        }
    }
}