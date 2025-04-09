namespace CloudPhotoSync.Service
{
    public record BlobEventData(
        string ETag,
        int ContentLength,
        string Url
    );
}
