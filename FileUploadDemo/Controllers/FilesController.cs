using FileUploadDemo.FileUpload;
using FileUploadDemo.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileUploadDemo.Controllers
{
    [Produces("application/json")]
    [Route("api/files")]
    public class FilesController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IFileMetadataRepository _fileMetadataRepository;

        public FilesController(IConfiguration configuration, IFileMetadataRepository fileMetadataRepository)
        {
            _configuration = configuration;
            _fileMetadataRepository = fileMetadataRepository;
        }

        [HttpGet]
        public IList<FileViewModel> GetAll()
        {
            return _fileMetadataRepository
                .GetAll()
                .Select(f =>ToFileViewModel(f)).ToList();
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IList<IFormFile> files)
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

                    await FileUploadManager.AddFileBlockAsync(_configuration, fileBlockInfo, contentStream, false);
                }
            }

            return Ok();
        }

        [HttpPost("aggregate/{fileId}")]
        public async Task<FileViewModel> AggregateBlocks(string fileId)
        {
            var fileMetadata = await FileUploadManager.CompleteUploadAsync(fileId, _fileMetadataRepository);

            return ToFileViewModel(fileMetadata);
        }

        [HttpDelete]
        public void DeleteFiles(FileIdsModel model)
        {
            FileUploadManager.DeleteFiles(_configuration, _fileMetadataRepository, model.FileIds);
        }

        private FileViewModel ToFileViewModel(FileMetadata fileMetadata)
        {
            return new FileViewModel
            {
                Id = fileMetadata.Id,
                Name = fileMetadata.FileName,
                size = fileMetadata.FileSize,
                CreatedDateUtc = fileMetadata.CreateDateUtc
            };
        }
    }
}