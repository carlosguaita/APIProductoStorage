using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace APIProductoStorage
{

    public class APIProductoStorage
    {
        private readonly ILogger<APIProductoStorage> _logger;

        public APIProductoStorage(ILogger<APIProductoStorage> logger)
        {
            _logger = logger;
        }

        [Function("UploadBlob")]
        public async Task<IActionResult> PostBlob(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route ="productostorage")]
            HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request POST. ");
            try
            {
                string Connection = Environment.GetEnvironmentVariable("StorageConnectionString");
                Stream myBlob = new MemoryStream();
                var formdata = await req.ReadFormAsync();
                var file = formdata.Files["file"];
                if (file == null)
                {
                    return new BadRequestObjectResult("Not File");
                }
                var blobName = $"{Guid.NewGuid().ToString()}{Path.GetExtension(file.FileName)}";
                myBlob = file.OpenReadStream();
                var blobClient = new BlobContainerClient(Connection, "producto-images");
                var blob = blobClient.GetBlobClient(blobName);
                //await blob.UploadAsync(myBlob);
                await blob.UploadAsync(
                   myBlob,
                   new BlobHttpHeaders
                   {
                       ContentType = file.ContentType
                   },
                   conditions: null
                   );
                return new OkObjectResult(blobName);
            }catch(Exception ex)
            {
                return new BadRequestObjectResult("Not File");
            }
        }

        [Function("GetBlobUrl")]
        public async Task<IActionResult> GetBlob(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route ="productostorage/{blobname}")]
            HttpRequest req,
            string blobname)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request GET BY NAME.");
            try
            {
                string Connection = Environment.GetEnvironmentVariable("StorageConnectionString");

                var blobClient = new BlobContainerClient(Connection, "producto-images");

                var blob = blobClient.GetBlobClient(blobname);

                BlobSasBuilder blobSasBuilder = new()
                {
                    BlobContainerName = blob.BlobContainerName,
                    BlobName = blob.Name,
                    ExpiresOn = DateTime.UtcNow.AddHours(2),
                    Protocol = SasProtocol.Https,
                    Resource = "b"
                };
                blobSasBuilder.SetPermissions(BlobAccountSasPermissions.Read);
               
               
                return new OkObjectResult(blob.GenerateSasUri(blobSasBuilder).ToString());
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult("Not Exist");
            }
        }

        [Function("DeleteBlob")]
        public async Task<IActionResult> DeleteBlob(
           [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route ="productostorage/{blobname}")]
            HttpRequest req,
           string blobname)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request DELETE.");
            try
            {
                string Connection = Environment.GetEnvironmentVariable("StorageConnectionString");

                var blobClient = new BlobContainerClient(Connection, "producto-images");

                var blob = blobClient.GetBlobClient(blobname);

                await blob.DeleteIfExistsAsync(Azure.Storage.Blobs.Models.DeleteSnapshotsOption.IncludeSnapshots);

                return new OkObjectResult("Blob Deleted");
            }
            catch (Exception ex)
            {
                return new BadRequestObjectResult("Not Exist");
            }
        }
    }
}

