﻿using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileUploadDemo.FileUpload
{
    public class FileUploader
    {
        private const string FileBlockExtension = ".block";
        private readonly string _storageDirectory;
        private readonly FileMetadata _fileMetadata;

        private int _initialized = 0;
        private int _initializing = 0;


        public FileUploader(IConfiguration configuration)
        {
            _storageDirectory = configuration.GetValue<string>("FileStoreDirectory");
            _fileMetadata = new FileMetadata();
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

        public async Task AggregateBlocksAsync()
        {
            var allBlocks = Directory.EnumerateFiles(_fileMetadata.Location, $"*{FileBlockExtension}")?.OrderBy(b => b);

            if (!allBlocks.Any())
            {
                throw new InvalidOperationException("No file blocks available");
            }

            var filePath = Path.Combine(_fileMetadata.Location, _fileMetadata.FileName);

            using (var fileStream = File.Create(filePath))
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

        private async Task DoUploadFileBlock(FileBlockInfo fileBlockInfo, Stream fileContent)
        {
            var filename = GetFileBlockName(fileBlockInfo);
            var filepath = Path.Combine(_fileMetadata.Location, filename);

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
                    //_directory = Path.Combine(_storageDirectory, fileBlockInfo.FileId);
                    _fileMetadata.Id = fileBlockInfo.FileId;
                    _fileMetadata.FileName = fileBlockInfo.FileName;
                    _fileMetadata.FileSize = fileBlockInfo.FileSize;
                    _fileMetadata.Location = Path.Combine(_storageDirectory, fileBlockInfo.FileId);
                    //_fileMetadata.ContentType = fileBlockInfo


                    if (!Directory.Exists(_fileMetadata.Location))
                    {
                        Directory.CreateDirectory(_fileMetadata.Location);
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
