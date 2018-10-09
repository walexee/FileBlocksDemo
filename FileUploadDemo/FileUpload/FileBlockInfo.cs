namespace FileUploadDemo.FileUpload
{
    public class FileBlockInfo
    {
        public string FileId { get; set; }

        public string BlockId { get; set; }

        public string FileName { get; set; }

        public int SequenceNum { get; set; }

        public long FileSize { get; set; }

        public long BlockSize { get; internal set; }

        public int TotalBlocksCount { get; set; }
    }
}
