using Azure.Messaging.ServiceBus;
using System.Linq;
using System.Reactive.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System;

namespace SendCameraCaptureEvent
{
    public class ServiceBusReceiver<T>
    {
        private readonly ServiceBusReceiver receiver;

        public ServiceBusReceiver(ServiceBusReceiver receiver)
        {
            this.receiver = receiver;
        }

        public IObservable<T> GetEventStream()
        {
            return receiver
                .ReceiveMessagesAsync()
                .Select(m => m.Body.ToString())
                .Select(JsonConvert.DeserializeObject<T>)
                .ToObservable();
        }
    }
}
