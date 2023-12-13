using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace SMARTCustomOperations.Export.Services
{
    public class ExportFileService : IExportFileService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger _logger;

        const string ContainerName = "export";

        public ExportFileService(BlobServiceClient blobServiceClient, ILogger<ExportFileService> logger)
        {
            _blobServiceClient = blobServiceClient;
            _logger = logger;
        }

        public async Task<byte[]> GetContent(string containerName, string blobName)
        {
            BlobClient blobClient;

            try
            {
                // special case, these types are not included in the
                // Azure FHIR service export
                System.Text.RegularExpressions.Regex re = new System.Text.RegularExpressions.Regex(@".+(Practitioner|Organization|Device).+");
                _logger.LogInformation("Matching {blobName}", blobName);
                if ( re.Match(blobName).Success )
                {
                    _logger.LogInformation("Special handling for {blobName}", blobName);
                    return await GetSpecial(blobName);
                }

                // Get the blob client for the export file
                blobClient = _blobServiceClient
                    .GetBlobContainerClient(containerName)
                    .GetBlobClient(blobName);

                if (!blobClient.Exists())
                {
                    throw new ArgumentException($"Blob {blobName} does not exist in container {containerName}");
                }

                var blobContent = await blobClient.DownloadContentAsync();
                BinaryData blobBinaryContent = blobContent.Value.Content!;
                return blobBinaryContent.ToArray();
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 401)
            {
                throw new SystemException("Backend service is not authorized to access the blob. Check your configuration.", ex);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                throw new ArgumentException($"Blob {blobName} does not exist in container {containerName}");
            }
        }

        private async Task<byte[]> GetSpecial(string originalBlobName)
        {
            BlobClient blobClient;
            string blobName = "none";
            System.Text.RegularExpressions.Regex re = new System.Text.RegularExpressions.Regex(@"\d+\-\d+\/(\w+)\-\d+\-\d+\.ndjson");
            var match = re.Match(originalBlobName);
            if (match.Success)
            {
                blobName = $"{match.Groups[1].Captures[0].Value}.ndjson";
                _logger.LogInformation("Remapping {originalBlobName} into {blobName}", originalBlobName, blobName);
            }

            var containerName = ContainerName;

            try
            {
                // Get the blob client for the export file
                blobClient = _blobServiceClient
                    .GetBlobContainerClient(containerName)
                    .GetBlobClient(blobName);

                if (!blobClient.Exists())
                {
                    throw new ArgumentException($"Blob {blobName} does not exist in container {containerName}");
                }

                var blobContent = await blobClient.DownloadContentAsync();
                BinaryData blobBinaryContent = blobContent.Value.Content!;
                return blobBinaryContent.ToArray();
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 401)
            {
                throw new SystemException("Backend service is not authorized to access the blob. Check your configuration.", ex);
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 404)
            {
                throw new ArgumentException($"Blob {blobName} does not exist in container {containerName}");
            }
        }
    }
}
