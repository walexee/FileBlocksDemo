using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileUploadDemo.FileUpload
{
    public class AzureFileUploader : IFileUploader
    {
        private readonly IConfiguration _configuration;
        private readonly IAzureAccountManager _azureAccountManager;
        private readonly FileMetadata _fileMetadata;
        

        private int _initialized = 0;
        private int _initializing = 0;

        private CloudBlockBlob _blockBlobReference;
        private int _blocksCount;

        public AzureFileUploader(IConfiguration configuration, IAzureAccountManager azureAccountManager)
        {
            _configuration = configuration;
            _azureAccountManager = azureAccountManager;
            _fileMetadata = new FileMetadata();
        }

        public async Task UploadFileBlockAsync(FileBlockInfo fileBlockInfo, Stream fileContent)
        {
            await Initialize(fileBlockInfo);

            while (_initialized == 0)
            {
                Thread.Sleep(50);
            }

            await DoUploadFileBlockAsync(fileBlockInfo, fileContent);
        }

        public FileMetadata GetFileMetadata()
        {
            return _fileMetadata;
        }

        public async Task<FileMetadata> CompleteUploadAsync()
        {
            if (_blocksCount == 1)
            {
                return _fileMetadata;
            }

            var blockIds = Enumerable.Range(1, _blocksCount).Select(b => ToBase64(b)).ToList();

            await _blockBlobReference.PutBlockListAsync(blockIds);

            return _fileMetadata;
        }

        private async Task DoUploadFileBlockAsync(FileBlockInfo fileBlockInfo, Stream fileContent)
        {
            if (_blocksCount == 1)
            {
                await _blockBlobReference.UploadFromStreamAsync(fileContent);
                return;
            }

            var blockId = ToBase64(fileBlockInfo.SequenceNum);

            await _blockBlobReference.PutBlockAsync(blockId, fileContent, null);
        }

        private async Task Initialize(FileBlockInfo fileBlockInfo)
        {
            if (0 == Interlocked.Exchange(ref _initializing, 1))
            {
                try
                {
                    _fileMetadata.Id = Guid.NewGuid();
                    _fileMetadata.FileName = fileBlockInfo.FileName;
                    _fileMetadata.FileSize = fileBlockInfo.FileSize;
                    _fileMetadata.Location = _fileMetadata.Id.ToString();
                    _fileMetadata.CreateDateUtc = DateTime.UtcNow;
                    _fileMetadata.Store = FileStore.Azure;

                    _blocksCount = fileBlockInfo.TotalBlocksCount;

                    _blockBlobReference = await _azureAccountManager.GetBlobReferenceAsync(_fileMetadata);
                }
                finally
                {
                    Interlocked.Exchange(ref _initialized, 1);
                }
            }
        }

        private string ToBase64(int value)
        {
            var paddedValue = value.ToString().PadLeft(15, '0');

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(paddedValue));
        }
    }
}
