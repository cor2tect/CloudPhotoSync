using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

namespace CloudPhotoSync.Service
{
    public interface IObjectStore
    {
        Task<bool> ExistsAsync(string path);

        Task<ObjectMetaData> GetMetaDataAsync(string path);

        IAsyncEnumerable<ObjectMetaData> GetMetaDataSet(string prefix, IEnumerable<string> folders);

        IAsyncEnumerable<string> GetPaths(string prefix, IEnumerable<string> folders);

        Task<Stream> ReadAsync(string path);

        Task<long> WriteAsync(string path, Stream source);

        Task DeleteAsync(string path);
    }
}
