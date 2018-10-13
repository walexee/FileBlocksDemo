using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace FileUploadDemo.FileUpload
{
    public class FileMetadataRepository : IFileMetadataRepository, IDisposable
    {
        private const string StoreFileName = "files_metadata.json";
        private readonly string StoreFilePath;
        private readonly ConcurrentDictionary<Guid, FileMetadata> FileMetadataStore;

        private readonly System.Timers.Timer _timer;
        private const int _timerInterval = 1000; // milliseconds

        private static int _syncing = 0;
        private static int _storeInitialized = 0;

        public FileMetadataRepository(IConfiguration configuration)
        {
            StoreFilePath = Path.Combine(configuration.GetValue<string>("FileStoreDirectory"), StoreFileName);

            FileMetadataStore = new ConcurrentDictionary<Guid, FileMetadata>();

            _timer = new System.Timers.Timer(_timerInterval);
            _timer.Elapsed += SyncFileStore;
        }

        public FileMetadata Get(Guid fileId)
        {
            EnsureStoreIsInitialized();
            FileMetadataStore.TryGetValue(fileId, out var fileInfo);

            return fileInfo;
        }

        public ICollection<FileMetadata> GetAll()
        {
            EnsureStoreIsInitialized();

            return FileMetadataStore.Values;
        }

        public void Save(FileMetadata fileMetadata)
        {
            //EnsureStoreIsInitialized();
            FileMetadataStore.AddOrUpdate(fileMetadata.Id, fileMetadata, (key, value) => value);
            SyncFileStore(null, null);
        }

        public void Delete(Guid fileId)
        {
            FileMetadataStore.TryRemove(fileId, out var fileInfo);
            SyncFileStore(null, null);
        }

        public void Dispose()
        {
            _timer.Elapsed -= SyncFileStore;
            _timer.Dispose();
        }

        private void SyncFileStore(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (0 == Interlocked.Exchange(ref _syncing, 1))
            {
                EnsureStoreIsInitialized();
                var contentJson = JsonConvert.SerializeObject(FileMetadataStore.Values, Formatting.Indented);

                File.WriteAllText(StoreFilePath, contentJson);

                Interlocked.Exchange(ref _syncing, 0);
            }
        }

        private void EnsureStoreIsInitialized()
        {
            if (0 == Interlocked.Exchange(ref _storeInitialized, 1))
            {
                EnsureStoreFileExists();

                var storeContent = File.ReadAllText(StoreFilePath);

                if (!string.IsNullOrWhiteSpace(storeContent))
                {
                    var existingFileMetadatas = JsonConvert.DeserializeObject<List<FileMetadata>>(storeContent);

                    foreach (var fileMetadata in existingFileMetadatas)
                    {
                        FileMetadataStore.TryAdd(fileMetadata.Id, fileMetadata);
                    }
                }

                // start timer
                _timer.Enabled = true;
            }
        }

        private void EnsureStoreFileExists()
        {
            if (File.Exists(StoreFilePath))
            {
                return;
            }

            var directory = Path.GetDirectoryName(StoreFilePath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var fileStream = File.Create(StoreFilePath))
            {
                var content = Encoding.UTF8.GetBytes("[]");

                fileStream.Write(content, 0, content.Length);
                fileStream.Flush();
            }
        }
    }
}
