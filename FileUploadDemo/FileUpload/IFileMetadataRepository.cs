namespace FileUploadDemo.FileUpload
{
    public interface IFileMetadataRepository
    {
        FileMetadata Get(string fileId);

        void Save(FileMetadata fileInfo);

        void Delete(string fileId);
    }
}