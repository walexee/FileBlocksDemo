using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileUploadDemo.FileUpload
{
    public class FileUploader
    {
        
        private const string FileBlockExtension = ".block";
        private readonly string _storageDirectory;
        private int _initialized = 0;
        private int _initializing = 0;
        private string _directory;

        public FileUploader(IConfiguration configuration)
        {
            _storageDirectory = configuration.GetValue<string>("FileStoreDirectory");
        }

        public async Task UploadFileBlockAsync(FileBlockInfo fileBlockInfo, Stream fileContent)
        {
            Initialize(fileBlockInfo);

            while (_initialized == 0)
            {
                Thread.Sleep(50);
            }

            await DoUploadFileBlock(fileBlockInfo, fileContent);
        }

        public async Task AggregateBlocksAsync(FileBlockInfo info)
        {
            var allBlocks = Directory.EnumerateFiles(_directory, $"*{FileBlockExtension}");
            var filename = Path.Combine(_directory, info.FileName);

            using (var fileStream = File.Create(filename))
            {
                foreach (var block in allBlocks)
                {
                    using (var blockStream = File.OpenRead(block))
                    {
                        await blockStream.CopyToAsync(fileStream);
                    }
                }

                await fileStream.FlushAsync();
            }
        }

        public async Task CompleteUploadAsync(string fileId)
        {
            throw new NotImplementedException();
        }

        private async Task DoUploadFileBlock(FileBlockInfo fileBlockInfo, Stream fileContent)
        {
            var filename = GetFileBlockName(fileBlockInfo);
            var filepath = Path.Combine(_directory, filename);

            using (var fileStream = File.Create(filepath))
            {
                await fileContent.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
            }
        }

        private string GetFileBlockName(FileBlockInfo fileBlockInfo)
        {
            return $"{fileBlockInfo.SequenceNum.ToString().PadLeft(15, '0')}{FileBlockExtension}";
        }

        private void Initialize(FileBlockInfo fileBlockInfo)
        {
            if (0 == Interlocked.Exchange(ref _initializing, 1))
            {
                try
                {
                    _directory = Path.Combine(_storageDirectory, fileBlockInfo.FileId);

                    if (!Directory.Exists(_directory))
                    {
                        Directory.CreateDirectory(_directory);
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _initialized, 1);
                }
            }
        }
    }
}
