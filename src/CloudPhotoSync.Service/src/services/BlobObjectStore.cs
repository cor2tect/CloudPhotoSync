using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace CloudPhotoSync.Service.services
{
    public class BlobObjectStore : IObjectStore
    {
        private readonly BlobContainerClient containerClient;
        private readonly ILogger<BlobObjectStore> logger;

        public BlobObjectStore(
            BlobContainerClient containerClient,
            ILogger<BlobObjectStore> logger
        )
        {
            this.containerClient = containerClient;
            this.logger = logger;
        }

        private BlobClient GetBlobClient(string path) =>
            containerClient.GetBlobClient(path);

        public Task<bool> ExistsAsync(string path)
        {
            return GetBlobClient(path)
                .ExistsAsync()
                .GetValueAsync();
        }

        public async Task<ObjectMetaData> GetMetaDataAsync(string path)
        {
            var properties = await GetBlobClient(path)
                .GetPropertiesAsync()
                .GetValueAsync();

            return new ObjectMetaData(
                Hash: properties.ContentHash,
                LastWrite: properties.LastModified,
                Path: path,
                Length: properties.ContentLength
            );
        }

        public async IAsyncEnumerable<string> GetPaths(string prefix, IEnumerable<string> folders)
        {
            await foreach (var s in GetMetaDataSet(prefix, folders).Select(m => m.Path)) yield return s;
        }
        
        public async IAsyncEnumerable<ObjectMetaData> GetMetaDataSet(string prefix, IEnumerable<string> folders)
        {
            static ObjectMetaData ToObjectMeta(BlobItem blobItem) =>
                new(
                    Hash: blobItem.Properties.ContentHash,
                    LastWrite: blobItem.Properties.LastModified ?? new DateTimeOffset(),
                    Path: blobItem.Name,
                    Length: blobItem.Properties.ContentLength ?? 0
                );
            
            foreach (var folder in folders)
            {
                await foreach (var blob in containerClient
                    .GetBlobsAsync(prefix: prefix)
                    .AsPages()
                    .SelectMany(b => b.Values.ToAsyncEnumerable())
                    .Where(b => b.Name.Contains($"/{folder}/") || 
                                (folder == "/" && 
                                 Regex.IsMatch(b.Name[..b.Name.LastIndexOf('/')].Split(new [] { @"/" }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty, @".*-.*-[\d]{8,8}-[\d]{1,}")))
                    .Select(ToObjectMeta))
                {
                    yield return blob;
                }
            }

        }

        public async Task<Stream> ReadAsync(string path)
        {
            var info = await GetBlobClient(path)
                .DownloadAsync()
                .GetValueAsync();

            return info.Content;
        }

        public async Task<long> WriteAsync(string path, Stream stream)
        {
            await GetBlobClient(path)
                .UploadAsync(
                    content: stream,
                    overwrite: true
                )
                .GetValueAsync();

            return stream.Length;
        }

        public Task DeleteAsync(string path)
        {
            var result =  GetBlobClient(path).DeleteIfExistsAsync();
          
            return result;
        }
    }
}
