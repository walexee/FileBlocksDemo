using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FileUploadDemo.FileUpload
{
    public static class FileUploadManager
    {
        private static ConcurrentDictionary<string, IFileUploader> _uploaders = new ConcurrentDictionary<string, IFileUploader>();

        public static async Task AddFileBlockAsync(IConfiguration configuration, FileBlockInfo fileBlockInfo, Stream stream, bool sendToAzure)
        {
            _uploaders.TryGetValue(fileBlockInfo.FileId, out var uploader);

            if (uploader == null)
            {
                //if (throwIfUploadNotFound)
                //{
                //    throw new KeyNotFoundException($"No file block uploader found for file Id {fileBlockInfo.FileId}");
                //}

                await AddOrInitializeUploadAsync(configuration, fileBlockInfo, stream, sendToAzure);
                return;
            }

            await uploader.UploadFileBlockAsync(fileBlockInfo, stream);
        }

        public static async Task AddOrInitializeUploadAsync(IConfiguration configuration, FileBlockInfo fileBlockInfo, Stream stream, bool sendToAzure)
        {
            var uploader = _uploaders.GetOrAdd(fileBlockInfo.FileId, key =>
            {
                if (sendToAzure)
                {
                    return new AzureFileUploader(configuration);
                }

                return new FileUploader(configuration);
            });

            await uploader.UploadFileBlockAsync(fileBlockInfo, stream);
        }

        public static async Task<FileMetadata> CompleteUploadAsync(string fileId, IFileMetadataRepository fileMetadataRepository)
        {
            _uploaders.TryGetValue(fileId, out var uploader);

            if (uploader == null)
            {
                throw new KeyNotFoundException($"No uploader found for file Id {fileId}");
            }

            try
            {
                var fileMetadata = await uploader.CompleteUploadAsync();

                fileMetadataRepository.Save(fileMetadata);

                return fileMetadata;
            }
            finally
            {
                _uploaders.TryRemove(fileId, out uploader);
            }
        }

        // TODO: refactor
        public static void DeleteFiles(IConfiguration configuration, IFileMetadataRepository repository, IEnumerable<Guid> fileIds)
        {
            var storageDirectory = configuration.GetValue<string>("FileStoreDirectory");

            foreach (var fileId in fileIds)
            {
                var fileDirectory = Path.Combine(storageDirectory, fileId.ToString());

                repository.Delete(fileId);

                Directory.Delete(fileDirectory, true);
            }
        }
    }
}
