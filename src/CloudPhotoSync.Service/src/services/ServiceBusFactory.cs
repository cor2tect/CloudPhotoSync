using System;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Logging;

namespace CloudPhotoSync.Service
{
    public class ServiceBusFactory
    {
        private readonly Lazy<ServiceBusClient> client;

        private readonly Lazy<ManagementClient> managementClient;

        private readonly ILoggerFactory loggerFactory;

        public ServiceBusFactory(
            ServiceBusOptions options,
            ILoggerFactory loggerFactory
        )
        {
            ServiceBusClient CreateClient() =>
               new(options.ConnectionString);

            ManagementClient CreateManagementClient() =>
                new(options.ConnectionString);

            this.client = new Lazy<ServiceBusClient>(CreateClient);
            this.managementClient = new Lazy<ManagementClient>(CreateManagementClient);
            this.loggerFactory = loggerFactory;
        }

        public async Task<ServiceBusReceiver<T>> GetEventReceiverAsync<T>(
            string requestTopicName,
            string subscriptionName
        )
        {
            var options = new ServiceBusReceiverOptions()
            {
                ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
            };

            await CreateSubscriptionIfNecessaryAsync(
                topicName: requestTopicName,
                subscriptionName: subscriptionName
            );

            var receiver = client.Value.CreateReceiver(
                topicName: requestTopicName,
                subscriptionName: subscriptionName,
                options: options
            );

            return new ServiceBusReceiver<T>(
                receiver: receiver,
                logger: loggerFactory.CreateLogger<ServiceBusReceiver<T>>()
            );
        }

        public async Task<ServiceBusSender> GetEventSender<T>(
            string responseTopicName,
            string subscriptionName)
        {
            await CreateSubscriptionIfNecessaryAsync(
                topicName: responseTopicName,
                subscriptionName: subscriptionName
            );

            var sender = client.Value.CreateSender(
                queueOrTopicName: responseTopicName
            );
            return sender;
        }

        private async Task CreateSubscriptionIfNecessaryAsync(string topicName, string subscriptionName)
        {
            var mgmCln = managementClient.Value;

            if (!await mgmCln.SubscriptionExistsAsync(topicName, subscriptionName))
            {
                await mgmCln.CreateSubscriptionAsync(
                    topicPath: topicName,
                    subscriptionName: subscriptionName
                );
            }
        }
    }
}
