
namespace CloudPhotoSync.Service.model
{
    public record DeviceEvent(
        DeviceEventType Result,
        string Message
    );
}
