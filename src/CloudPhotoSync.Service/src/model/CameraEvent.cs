namespace CloudPhotoSync.Service.model
{
    public record CameraEvent(
        CameraEventType Command,
        string BlobPath
    );
}
