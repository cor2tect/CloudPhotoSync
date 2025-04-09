using Azure.Messaging.ServiceBus;
using System.Reactive.Linq;
using System.Linq;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System;

namespace CloudPhotoSync.Service
{
    public record FileEventOptions(string RootDirectory);

    public class ServiceBusReceiver<T>
    {
        private readonly ServiceBusReceiver receiver;
        private readonly ILogger<ServiceBusReceiver<T>> logger;

        public ServiceBusReceiver(
            ServiceBusReceiver receiver,
            ILogger<ServiceBusReceiver<T>> logger
        )
        {
            this.receiver = receiver;
            this.logger = logger;
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
