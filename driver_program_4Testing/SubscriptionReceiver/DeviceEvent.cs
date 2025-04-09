using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubscriptionReceiver
{
    public record DeviceEvent(
        DeviceEventType Result,
        string Message
    );

    public enum DeviceEventType
    {
        DeviceNotFound,
        Ok,
        MiscError
    }
}
