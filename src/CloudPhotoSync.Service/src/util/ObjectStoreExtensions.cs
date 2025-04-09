using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CloudPhotoSync.Service.util
{
    public static class ObjectStoreExtensions
    {
        public static async Task<long> CopyObjectAsync(
            this IObjectStore sourceStore,
            IObjectStore targetStore,
            string path
        )
        {
            await using var sourceStream = await sourceStore.ReadAsync(path);
            return await targetStore.WriteAsync(path, sourceStream);
        }

        public static async Task<CopyResult> CopyIfDifferentAsync(this IObjectStore sourceStore,
            IObjectStore targetStore,
            string path)
        {
            Task<long> Copy() =>  sourceStore.CopyObjectAsync(targetStore, path);

            if (!await targetStore.ExistsAsync(path))
            {
                var bytes = await Copy();
                return new CopyResult()
                {
                    BytesTransferred = bytes,
                    Copied = true
                };
            }

            var sourceMeta = await sourceStore.GetMetaDataAsync(path);
            var targetMeta = await targetStore.GetMetaDataAsync(path);

            if (!sourceMeta.Hash.SequenceEqual(targetMeta.Hash))
            {
                var bytes = await Copy();
                return new CopyResult()
                {
                    BytesTransferred = bytes,
                    Copied = true
                };
            }

            return new CopyResult();
        }

        public static IAsyncEnumerable<string> CopyObjectSetAsync(
            this IObjectStore sourceStore,
            IObjectStore targetStore,
            string prefix,
            IEnumerable<string> folders
        )
        {
            async ValueTask<bool> Copy(string path) =>
                (await sourceStore.CopyIfDifferentAsync(targetStore, path).ConfigureAwait(false)).Copied;

            return sourceStore
                 .GetPaths(prefix, folders)
                 .WhereAwait(Copy);
        }

        public static async Task<bool> DeleteIfExistsAsync(
            this IObjectStore targetStore,
            string path
        )
        {
            if (!await targetStore.ExistsAsync(path)) return false;
            await targetStore.DeleteAsync(path);
            return true;
        }
    }

    public class CopyResult
    {
        public bool Copied { get; set; } = false;
        public long BytesTransferred { get; set; } = 0;
    }
}
