using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FileUploadDemo.FileUpload
{
    public class FileUploadManager : IFileUploadManager
    {
        private ConcurrentDictionary<string, IFileUploader> _uploaders = new ConcurrentDictionary<string, IFileUploader>();

        private readonly IFileMetadataRepository _fileMetadataRepository;
        private readonly IAzureAccountManager _azureAccountManager;
        private readonly IConfiguration _configuration;

        public FileUploadManager(
            IFileMetadataRepository fileMetadataRepository, 
            IAzureAccountManager azureAccountManager,
            IConfiguration configuration)
        {
            _fileMetadataRepository = fileMetadataRepository;
            _azureAccountManager = azureAccountManager;
            _configuration = configuration;
        }

        public async Task AddFileBlockAsync(IServiceProvider serviceProvider, FileBlockInfo fileBlockInfo, Stream stream, bool sendToAzure)
        {
            _uploaders.TryGetValue(fileBlockInfo.FileId, out var uploader);

            if (uploader == null)
            {
                await AddOrInitializeUploadAsync(serviceProvider, fileBlockInfo, stream, sendToAzure);
                return;
            }

            await uploader.UploadFileBlockAsync(fileBlockInfo, stream);
        }

        public async Task AddOrInitializeUploadAsync(IServiceProvider serviceProvider, FileBlockInfo fileBlockInfo, Stream stream, bool sendToAzure)
        {
            var uploader = _uploaders.GetOrAdd(fileBlockInfo.FileId, key =>
            {
                if (sendToAzure)
                {
                    return new AzureFileUploader(_configuration, _azureAccountManager);
                }

                return new FileUploader(_configuration);
            });

            await uploader.UploadFileBlockAsync(fileBlockInfo, stream);
        }

        public async Task<FileMetadata> CompleteUploadAsync(string fileUId)
        {
            _uploaders.TryGetValue(fileUId, out var uploader);

            if (uploader == null)
            {
                throw new KeyNotFoundException($"No uploader found for file Id {fileUId}");
            }

            try
            {
                var fileMetadata = await uploader.CompleteUploadAsync();

                _fileMetadataRepository.Save(fileMetadata);

                return fileMetadata;
            }
            finally
            {
                _uploaders.TryRemove(fileUId, out uploader);
            }
        }

        public async Task DeleteFilesAsync(IEnumerable<Guid> fileIds)
        {
            var storageDirectory = _configuration.GetValue<string>("FileStoreDirectory");

            foreach (var fileId in fileIds)
            {
                var file = _fileMetadataRepository.Get(fileId);
                var fileDirectory = Path.Combine(storageDirectory, fileId.ToString());

                if (Directory.Exists(fileDirectory))
                {
                    Directory.Delete(fileDirectory, true);
                }

                if (file == null)
                {
                    continue;
                }

                if (file.Store == FileStore.Azure)
                {
                    var blobReference = await _azureAccountManager.GetBlobReferenceAsync(file);
                    await blobReference.DeleteIfExistsAsync();
                }

                _fileMetadataRepository.Delete(file.Id);
            }
        }

        public async Task<Stream> GetFileContentAsync(FileMetadata fileMetadata)
        {
            if (fileMetadata.Store == FileStore.FileSystem)
            {
                var filePath = Path.Combine(fileMetadata.Location, fileMetadata.FileName);

                return File.OpenRead(filePath);
            }

            var blobReference = await _azureAccountManager.GetBlobReferenceAsync(fileMetadata);

            return await blobReference.OpenReadAsync();
        }

        public Task<string> GetAzureFileDownloadLinkAsync(FileMetadata fileMetadata)
        {
            return _azureAccountManager.GetFileDownloadUrlAsync(fileMetadata);
        }

        public async Task CancelUploadsAsync(IEnumerable<string> fileUIds)
        {
            foreach(var uid in fileUIds)
            {
                if (!_uploaders.TryRemove(uid, out var uploader))
                {
                    continue;
                }

                var fileMetadata = uploader.GetFileMetadata();

                await DeleteFilesAsync(new Guid[] { fileMetadata.Id });
            }
        }
    }

    public interface IFileUploadManager
    {
        Task AddFileBlockAsync(IServiceProvider serviceProvider, FileBlockInfo fileBlockInfo, Stream stream, bool sendToAzure);

        Task AddOrInitializeUploadAsync(IServiceProvider serviceProvider, FileBlockInfo fileBlockInfo, Stream stream, bool sendToAzure);

        Task<FileMetadata> CompleteUploadAsync(string fileId);

        Task DeleteFilesAsync(IEnumerable<Guid> fileIds);

        Task<Stream> GetFileContentAsync(FileMetadata fileMetadata);

        Task<string> GetAzureFileDownloadLinkAsync(FileMetadata fileMetadata);

        Task CancelUploadsAsync(IEnumerable<string> fileUIds);
    }
}
