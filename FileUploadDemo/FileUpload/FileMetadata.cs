using System;

namespace FileUploadDemo.FileUpload
{
    public class FileMetadata
    {
        public Guid Id { get; set; }

        public string FileName { get; set; }

        public string Location { get; set; }

        public long FileSize { get; set; }

        public DateTime CreateDateUtc { get; set; }

        public FileStore Store { get; set; }
    }
}
