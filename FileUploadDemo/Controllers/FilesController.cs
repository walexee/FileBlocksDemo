using FileUploadDemo.FileUpload;
using FileUploadDemo.Models;
using Microsoft.AspNetCore.Mvc;
using MimeMapping;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace FileUploadDemo.Controllers
{
    [Produces("application/json")]
    [Route("api/files")]
    public class FilesController : Controller
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IFileMetadataRepository _fileMetadataRepository;
        private readonly IFileUploadManager _fileUploadManager;

        public FilesController(
            IServiceProvider serviceProvider, 
            IFileMetadataRepository fileMetadataRepository,
            IFileUploadManager fileUploadManager)
        {
            _serviceProvider = serviceProvider;
            _fileMetadataRepository = fileMetadataRepository;
            _fileUploadManager = fileUploadManager;
        }

        [HttpGet]
        public IList<FileViewModel> GetAll()
        {
            return _fileMetadataRepository
                .GetAll()
                .OrderBy(f => f.CreateDateUtc)
                .Select(f =>ToFileViewModel(f)).ToList();
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile()
        {
            foreach (var file in Request.Form.Files)
            {
                if (file.Length == 0)
                {
                    continue;
                }

                using (var contentStream = file.OpenReadStream())
                {
                    var fileBlockInfo = new FileBlockInfo
                    {
                        FileId = Request.Form["flowIdentifier"],
                        BlockId = Request.Form["flowIdentifier"],
                        FileName = Request.Form["flowFilename"],
                        FileSize = long.Parse(Request.Form["flowTotalSize"]),
                        BlockSize = long.Parse(Request.Form["flowChunkSize"]),
                        SequenceNum = int.Parse(Request.Form["flowChunkNumber"]),
                        TotalBlocksCount = int.Parse(Request.Form["flowTotalChunks"])
                    };

                    var uploadToAzure = Convert.ToBoolean(Request.Form["toAzure"]);

                    await _fileUploadManager.AddFileBlockAsync(_serviceProvider, fileBlockInfo, contentStream, uploadToAzure);
                }
            }

            return Ok();
        }

        [HttpPost("aggregate/{fileId}")]
        public async Task<FileViewModel> AggregateBlocks(string fileId)
        {
            var fileMetadata = await _fileUploadManager.CompleteUploadAsync(fileId);

            return ToFileViewModel(fileMetadata);
        }

        [HttpPost("cancelUploads")]
        public Task CancelUploads(FileIdsModel<string> model)
        {
            return _fileUploadManager.CancelUploadsAsync(model.FileIds);
        }

        [HttpGet("download/{fileId}")]
        public async Task<IActionResult> DownloadSingle(Guid fileId)
        {
            var fileMetadata = _fileMetadataRepository.Get(fileId);

            if (fileMetadata.Store == FileStore.Azure)
            {
                var downloadUrl = await _fileUploadManager.GetAzureFileDownloadLinkAsync(fileMetadata);

                return RedirectPermanent(downloadUrl);
            }

            var fileStream = await _fileUploadManager.GetFileContentAsync(fileMetadata);
            var contentType = MimeUtility.GetMimeMapping(fileMetadata.FileName);

            return File(fileStream, contentType, fileMetadata.FileName);
        }

        [HttpGet("downloadAll")]
        public IActionResult DownloadMultiple([FromQuery]IEnumerable<Guid> fileIds)
        {
            var files = _fileMetadataRepository.GetAll(fileIds);

            if (!files.Any())
            {
                throw new KeyNotFoundException("No files found with the given IDs");
            }

            return new FileCallbackResult("application/octet-stream", async (outputStream, _) =>
            {
                using (var zipArchive = new ZipArchive(new WriteOnlyStreamWrapper(outputStream), ZipArchiveMode.Create))
                {
                    foreach (var file in files)
                    {
                        var zipEntry = zipArchive.CreateEntry(file.FileName);

                        using (var zipStream = zipEntry.Open())
                        using (var stream = await _fileUploadManager.GetFileContentAsync(file))
                        {
                            await stream.CopyToAsync(zipStream);
                            await zipStream.FlushAsync();
                        }
                    }
                }
            })
            {
                FileDownloadName = "MyZipfile.zip"
            };
        }

        [HttpDelete]
        public Task DeleteFiles(FileIdsModel<Guid> model)
        {
            return _fileUploadManager.DeleteFilesAsync(model.FileIds);
        }

        private FileViewModel ToFileViewModel(FileMetadata fileMetadata)
        {
            return new FileViewModel
            {
                Id = fileMetadata.Id,
                Name = fileMetadata.FileName,
                size = fileMetadata.FileSize,
                Store = fileMetadata.Store,
                CreatedDateUtc = fileMetadata.CreateDateUtc
            };
        }
    }
}