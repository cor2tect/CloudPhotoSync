using System;

namespace CloudPhotoSync.Service
{
    public record FileEvent(
        FileEventType Type,
        string Path
    );
}
