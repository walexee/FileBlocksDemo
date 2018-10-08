using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace FileUploadDemo.FileUpload
{
    public class FileMetadataRepository : IDisposable, IFileMetadataRepository
    {
        private readonly string _storageDirectory;
        private readonly ConcurrentDictionary<string, FileInfo> _fileStore;

        private readonly System.Timers.Timer _timer;
        private const int _timerInterval = 1000; // milliseconds

        private int _hasChanges = 0;

        public FileMetadataRepository(IConfiguration configuration)
        {
            _fileStore = new ConcurrentDictionary<string, FileInfo>();
            _storageDirectory = configuration.GetValue<string>("FileStoreDirectory");
            _timer = new System.Timers.Timer(_timerInterval);
            _timer.Elapsed += SyncFileStore;
        }

        public FileInfo GetFileInfoAsync(string fileId)
        {
            _fileStore.TryGetValue(fileId, out var fileInfo);

            return fileInfo;
        }

        public void SaveFileInfoAsync(FileInfo fileInfo)
        {
            _fileStore.AddOrUpdate(fileInfo.Id, fileInfo, (key, value) => value);

            Interlocked.Exchange(ref _hasChanges, 1);
        }

        public void DeleteFileAsync(string fileId)
        {
            _fileStore.TryRemove(fileId, out var fileInfo);

            Interlocked.Exchange(ref _hasChanges, 1);
        }

        private void SyncFileStore(object sender, System.Timers.ElapsedEventArgs e)
        {
            // TODO: sync the dictionary with the database
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _timer.Elapsed -= SyncFileStore;
            _timer.Dispose();
        }
    }
}
