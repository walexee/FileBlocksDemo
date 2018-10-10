using System;

namespace FileUploadDemo.FileUpload
{
    public interface IFileMetadataRepository
    {
        FileMetadata Get(Guid fileId);

        void Save(FileMetadata fileMetadata);

        void Delete(Guid fileId);
    }
}