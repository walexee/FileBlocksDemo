using System;
using System.Collections.Generic;

namespace FileUploadDemo.FileUpload
{
    public interface IFileMetadataRepository
    {
        FileMetadata Get(Guid fileId);

        ICollection<FileMetadata> GetAll();

        ICollection<FileMetadata> GetAll(IEnumerable<Guid> fileIds);

        void Save(FileMetadata fileMetadata);

        void Delete(Guid fileId);
    }
}