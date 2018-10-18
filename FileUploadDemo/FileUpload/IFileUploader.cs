using System.IO;
using System.Threading.Tasks;

namespace FileUploadDemo.FileUpload
{
    public interface IFileUploader
    {
        Task<FileMetadata> CompleteUploadAsync();

        Task UploadFileBlockAsync(FileBlockInfo fileBlockInfo, Stream fileContent);

        FileMetadata GetFileMetadata();
    }
}