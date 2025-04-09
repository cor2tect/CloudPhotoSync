using System.Collections.Generic;

namespace CloudPhotoSync.Service
{
    public record SyncServiceOptions(
        string Container,
        string Subscription,
        string RequestTopic,
        string ResponseTopic,
        string SyncPrefix,
        string SyncFolder
    );
}