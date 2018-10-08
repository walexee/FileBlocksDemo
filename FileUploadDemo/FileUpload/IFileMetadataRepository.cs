namespace FileUploadDemo.FileUpload
{
    public interface IFileMetadataRepository
    {
        void DeleteFileAsync(string fileId);
        void Dispose();
        FileInfo GetFileInfoAsync(string fileId);
        void SaveFileInfoAsync(FileInfo fileInfo);
    }
}