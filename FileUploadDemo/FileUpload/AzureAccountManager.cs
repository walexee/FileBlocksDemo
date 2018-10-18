using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FileUploadDemo.FileUpload
{
    public class AzureAccountManager : IAzureAccountManager
    {
        private readonly IConfiguration _configuration;

        public AzureAccountManager(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<CloudBlockBlob> GetBlobReferenceAsync(FileMetadata fileMetadata)
        {
            var container = await GetContainerAsync();
            var filePath = Path.Combine(fileMetadata.Id.ToString(), fileMetadata.FileName);

            return container.GetBlockBlobReference(filePath);
        }

        public async Task<string> GetFileDownloadUrlAsync(FileMetadata fileMetadata)
        {
            var blobReference = await GetBlobReferenceAsync(fileMetadata);
            var policy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddMinutes(-15),
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddMinutes(1440),
            };

            var token = blobReference.GetSharedAccessSignature(policy);
            var url = Uri.UnescapeDataString(blobReference.Uri.ToString());

            return $"{url}{token}";
        }

        private async Task<CloudBlobContainer> GetContainerAsync()
        {
            var connectionString = _configuration.GetValue<string>("AzureBlobConnectionString");
            var containerName = _configuration.GetValue<string>("AzureBlobContainerName");
            var storageAccount = GetStorageAccount(connectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();

            var requestOptions = new BlobRequestOptions
            {
                MaximumExecutionTime = TimeSpan.FromMinutes(60),
                ParallelOperationThreadCount = 5,
                SingleBlobUploadThresholdInBytes = 1024 * 1024 * 40,
                ServerTimeout = TimeSpan.FromMinutes(60),
            };

            var operationContext = new OperationContext
            {
                LogLevel = LogLevel.Verbose,
            };

            var blobContainer = blobClient.GetContainerReference(containerName);

            await blobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Blob, requestOptions, operationContext);

            var permissions = new BlobContainerPermissions
            {
                PublicAccess = BlobContainerPublicAccessType.Blob
            };

            await blobContainer.SetPermissionsAsync(permissions);

            return blobContainer;
        }

        private CloudStorageAccount GetStorageAccount(string connectionString)
        {
            if (!CloudStorageAccount.TryParse(connectionString, out var cloudStorageAccount))
            {
                throw new InvalidCastException("The given string cannot be cast into a valid cloud storage account.");
            }

            return cloudStorageAccount;
        }
    }


    public interface IAzureAccountManager
    {
        Task<CloudBlockBlob> GetBlobReferenceAsync(FileMetadata fileMetadata);

        Task<string> GetFileDownloadUrlAsync(FileMetadata fileMetadata);
    }
}
