using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FileUploadDemo.FileUpload
{
    public static class FileUploadManager
    {
        private static ConcurrentDictionary<string, FileUploader> _uploaders = new ConcurrentDictionary<string, FileUploader>();

        public static async Task AddFileBlockAsync(IConfiguration configuration, FileBlockInfo fileBlockInfo, Stream stream, bool throwIfUploadNotFound = true)
        {
            _uploaders.TryGetValue(fileBlockInfo.FileId, out var uploader);

            if (uploader == null)
            {
                if (throwIfUploadNotFound)
                {
                    throw new KeyNotFoundException($"No file block uploader found for file Id {fileBlockInfo.FileId}");
                }

                await AddOrInitializeUploadAsync(configuration, fileBlockInfo, stream);
                return;
            }

            await uploader.UploadFileBlockAsync(fileBlockInfo, stream);
        }

        public static async Task AddOrInitializeUploadAsync(IConfiguration configuration, FileBlockInfo fileBlockInfo, Stream stream)
        {
            var uploader = _uploaders.GetOrAdd(fileBlockInfo.FileId, key => new FileUploader(configuration));

            await uploader.UploadFileBlockAsync(fileBlockInfo, stream);
        }

        public static async Task CompleteUploadAsync(string fileId)
        {
            _uploaders.TryGetValue(fileId, out var uploader);

            if (uploader == null)
            {
                throw new KeyNotFoundException($"No uploader found for file Id {fileId}");
            }

            try
            {
                await uploader.CompleteUploadAsync(fileId);
            }
            finally
            {
                _uploaders.TryRemove(fileId, out uploader);
            }
        }
    }
}
