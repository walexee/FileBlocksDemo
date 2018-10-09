using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace FileUploadDemo.FileUpload
{
    public class FileMetadataRepository : IFileMetadataRepository, IDisposable
    {
        private const string StoreFileName = "files_metadata.json";
        private readonly string StoreFilePath;
        private readonly ConcurrentDictionary<string, FileMetadata> FileMetadataStore;

        private readonly System.Timers.Timer _timer;
        private const int _timerInterval = 1000; // milliseconds

        private int _hasChanges = 0;

        public FileMetadataRepository(IConfiguration configuration)
        {
            StoreFilePath = Path.Combine(configuration.GetValue<string>("FileStoreDirectory"), StoreFileName);

            var storeContent = LoadStoreContent();

            FileMetadataStore = new ConcurrentDictionary<string, FileMetadata>(storeContent);

            _timer = new System.Timers.Timer(_timerInterval);
            _timer.Elapsed += SyncFileStore;
        }

        public FileMetadata Get(string fileId)
        {
            FileMetadataStore.TryGetValue(fileId, out var fileInfo);

            return fileInfo;
        }

        public void Save(FileMetadata fileInfo)
        {
            FileMetadataStore.AddOrUpdate(fileInfo.Id, fileInfo, (key, value) => value);

            Interlocked.Exchange(ref _hasChanges, 1);
        }

        public void Delete(string fileId)
        {
            FileMetadataStore.TryRemove(fileId, out var fileInfo);

            Interlocked.Exchange(ref _hasChanges, 1);
        }

        public void Dispose()
        {
            _timer.Elapsed -= SyncFileStore;
            _timer.Dispose();
        }

        private void SyncFileStore(object sender, System.Timers.ElapsedEventArgs e)
        {
            var contentJson = JsonConvert.SerializeObject(FileMetadataStore.Values, Formatting.Indented);

            File.WriteAllText(StoreFilePath, contentJson);
        }

        private IDictionary<string, FileMetadata> LoadStoreContent()
        {
            var storeContent = File.ReadAllText(StoreFilePath);

            if (!string.IsNullOrWhiteSpace(storeContent))
            {
                var content = JsonConvert.DeserializeObject<List<FileMetadata>>(storeContent);

                return content.ToDictionary(x => x.Id);
            }

            return new Dictionary<string, FileMetadata>();
        }
    }
}
