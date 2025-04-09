using System;

namespace CloudPhotoSync.Service
{
    public record ObjectMetaData(
        byte[] Hash,
        DateTimeOffset LastWrite,
        string Path,
        long Length
    );
}
