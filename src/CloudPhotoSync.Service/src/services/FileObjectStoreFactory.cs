using Microsoft.Extensions.Logging;

namespace CloudPhotoSync.Service
{
    public class FileObjectStoreFactory
    {
        private readonly ILoggerFactory loggerFactory;

        public FileObjectStoreFactory(ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
        }

        public FileObjectStore GetObjectStore(string rootDirectory)
        {
            var options = new FileStorageOptions(
                RootDirectory: rootDirectory
            );

            return new FileObjectStore(
                options: options,
                logger: loggerFactory.CreateLogger<FileObjectStore>()
            );
        }
    }
}
