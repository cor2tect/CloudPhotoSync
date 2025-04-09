using System;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace CloudPhotoSync.Service.services
{
    public class BlobObjectStoreFactory
    {
        private readonly Lazy<BlobServiceClient> blobServiceClient;

        private readonly ILoggerFactory loggerFactory;

        public BlobObjectStoreFactory(
            CloudStorageOptions options,
            ILoggerFactory loggerFactory
        )
        {
            BlobServiceClient CreateServiceClient() =>
                new(options.ConnectionString);

            this.blobServiceClient = new Lazy<BlobServiceClient>(CreateServiceClient);
            this.loggerFactory = loggerFactory;
        }

        public BlobObjectStore GetObjectStore(string containerName)
        {
            var container = blobServiceClient.Value.GetBlobContainerClient(containerName);
            return new BlobObjectStore(
                containerClient: container,
                logger: loggerFactory.CreateLogger<BlobObjectStore>()
            );
        }
    }
}
