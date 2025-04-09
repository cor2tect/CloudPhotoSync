using System;
using System.Text.RegularExpressions;

namespace CloudPhotoSync.Service
{
    public static class BlobEventExtensions
    {
        private const string prefixPattern = "/blobServices/default/containers/(.*?)/blobs/";

        public static string GetObjectPath(this BlobEvent @event)
        {
            return Regex.Replace(@event.Subject, prefixPattern, string.Empty);
        }

        public static BlobEventType GetEventType(this BlobEvent @event)
        {
            return @event.EventType switch
            {
                "Microsoft.Storage.BlobCreated" => BlobEventType.Created,
                "Microsoft.Storage.BlobDeleted" => BlobEventType.Deleted,
                var e => throw new Exception("Unknown Event Type: " + e)
            };
        }
    }
}
