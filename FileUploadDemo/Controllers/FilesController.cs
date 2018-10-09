using FileUploadDemo.FileUpload;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FileUploadDemo.Controllers
{
    [Produces("application/json")]
    [Route("api/Files")]
    public class FilesController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IFileMetadataRepository _fileMetadataRepository;

        public FilesController(IConfiguration configuration, IFileMetadataRepository fileMetadataRepository)
        {
            _configuration = configuration;
            _fileMetadataRepository = fileMetadataRepository;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IList<IFormFile> files)
        {
            foreach (var file in files)
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

                    await FileUploadManager.AddFileBlockAsync(_configuration, fileBlockInfo, contentStream);
                }
            }

            return Ok();
        }

        [HttpPost("aggregate")]
        public Task AggregateBlocks([FromBody]string fileId)
        {
            return FileUploadManager.CompleteUploadAsync(fileId);
        }
    }
}