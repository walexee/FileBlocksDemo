using System;
using System.Collections.Generic;

namespace FileUploadDemo.FileUpload
{
    public interface IFileMetadataRepository
    {
        FileMetadata Get(Guid fileId);

        ICollection<FileMetadata> GetAll();

        void Save(FileMetadata fileMetadata);

        void Delete(Guid fileId);
    }
}