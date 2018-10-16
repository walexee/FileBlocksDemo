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

        public async Task<FileMetadata> CompleteUploadAsync(string fileId)
        {
            _uploaders.TryGetValue(fileId, out var uploader);

            if (uploader == null)
            {
                throw new KeyNotFoundException($"No uploader found for file Id {fileId}");
            }

            try
            {
                var fileMetadata = await uploader.CompleteUploadAsync();

                _fileMetadataRepository.Save(fileMetadata);

                return fileMetadata;
            }
            finally
            {
                _uploaders.TryRemove(fileId, out uploader);
            }
        }

        public void DeleteFiles(IEnumerable<Guid> fileIds)
        {
            var storageDirectory = _configuration.GetValue<string>("FileStoreDirectory");

            foreach (var fileId in fileIds)
            {
                var fileDirectory = Path.Combine(storageDirectory, fileId.ToString());

                _fileMetadataRepository.Delete(fileId);

                Directory.Delete(fileDirectory, true);
            }
        }

        public Stream GetFileContent(FileMetadata fileMetadata)
        {
            var filePath = Path.Combine(fileMetadata.Location, fileMetadata.FileName);

            return File.OpenRead(filePath);
        }

        public Task<string> GetAzureFileDownloadLinkAsync(FileMetadata fileMetadata)
        {
            return _azureAccountManager.GetFileDownloadUrlAsync(fileMetadata);
        }
    }

    public interface IFileUploadManager
    {
        Task AddFileBlockAsync(IServiceProvider serviceProvider, FileBlockInfo fileBlockInfo, Stream stream, bool sendToAzure);

        Task AddOrInitializeUploadAsync(IServiceProvider serviceProvider, FileBlockInfo fileBlockInfo, Stream stream, bool sendToAzure);

        Task<FileMetadata> CompleteUploadAsync(string fileId);

        void DeleteFiles(IEnumerable<Guid> fileIds);

        Stream GetFileContent(FileMetadata fileMetadata);

        Task<string> GetAzureFileDownloadLinkAsync(FileMetadata fileMetadata);
    }
}
