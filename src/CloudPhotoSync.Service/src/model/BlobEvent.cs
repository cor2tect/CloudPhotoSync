using System.Text.RegularExpressions;

namespace CloudPhotoSync.Service
{
    public record BlobEvent(
        string Topic,
        string Subject,
        string Id,
        string EventType,
        BlobEventData Data
    );
}
