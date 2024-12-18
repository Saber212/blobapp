using System.Net;
using Azure;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace blobapp
{
    public class imagePost_Blob
    {
        private readonly ILogger _logger;

        public imagePost_Blob(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<imagePost_Blob>();
        }

        [Function("imagePost_Blob")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var uploadRequest = await req.ReadFromJsonAsync<UploadRequest>();

            if (uploadRequest == null)
            {
                var badReq = req.CreateResponse(HttpStatusCode.BadRequest);
                await badReq.WriteStringAsync("Invalid json");
                return badReq;
            }
            if (string.IsNullOrEmpty(uploadRequest.FileName) || string.IsNullOrEmpty(uploadRequest.FileContentBase64))
            {
                var badReq = req.CreateResponse(HttpStatusCode.BadRequest);
                await badReq.WriteStringAsync("Filename and filecontent is required.");
                return badReq;
            }

            byte[] fileContent = Convert.FromBase64String(uploadRequest.FileContentBase64);

            string blobConnectionString = Environment.GetEnvironmentVariable("BlobConnectionString");
            string containerName = "upload-images";

            var blobServiceContainer = new BlobServiceClient(blobConnectionString);

            var containerClient = blobServiceContainer.GetBlobContainerClient(containerName);

            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(uploadRequest.FileName);

            using (var stream = new MemoryStream(fileContent))
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"File uploaded successfully: {blobClient.Uri}");
            return response;
        }
    }
}
