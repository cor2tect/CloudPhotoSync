using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace CloudPhotoSync.Service
{
    public interface IObjectMetaDataStore
    {
        Task<ObjectMetaData> GetMetaDataAsync(string path);

        Task SetMetaDataAsync(ObjectMetaData metaData);

        IAsyncEnumerable<ObjectMetaData> GetMetaDataSet(string prefix);
    }

}
