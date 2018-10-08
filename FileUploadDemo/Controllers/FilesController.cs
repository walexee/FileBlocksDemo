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
                    // TODO: add other properties
                    var fileBlockInfo = new FileBlockInfo { FileName = file.FileName, FileSize = file.Length };

                    await FileUploadManager.AddFileBlockAsync(_configuration, fileBlockInfo, contentStream);
                }
            }

            return Ok();
        }

        [HttpPost("complete_upload")]
        public Task UploadFile([FromBody]string fileId)
        {
            return FileUploadManager.CompleteUploadAsync(fileId);
        }
    }
}