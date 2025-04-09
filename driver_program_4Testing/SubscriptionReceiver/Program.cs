using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SubscriptionReceiver
{
    class Program
    {
        // connection string to your Service Bus namespace
        static string connectionString = "Endpoint=sb://xyzzzzz.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=YOUR_KEY";

        // name of the Service Bus topic
        static string topicName = "camera-response";

        // name of the subscription to the topic
        static string subscriptionName = Environment.MachineName; //"LAPTOP-ACFVMUC2";

        // the client that owns the connection and can be used to create senders and receivers
        static ServiceBusClient client;

        // the processor that reads and processes messages from the subscription
        static ServiceBusProcessor processor;

        // handle received messages
        static Task MessageHandler(ProcessMessageEventArgs args)
        {
            //DeviceEvent dt = ByteArrayToObject<DeviceEvent>(args.Message.Body.ToArray());
            //string body = args.Message.Body.ToString();

            DeviceEvent dt = JsonConvert.DeserializeObject<DeviceEvent>(args.Message.Body.ToString());
            
            Console.WriteLine($"Received result: {dt.Result} from subscription: {subscriptionName}");
            Console.WriteLine($"Received message: {dt.Message} from subscription: {subscriptionName}");

            return Task.CompletedTask;
        }

        // handle any errors when receiving messages
        static Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Convert a byte array to an Object of T, using Protobuf.
        /// </summary>
        public static T ByteArrayToObject<T>(byte[] arrBytes)
        {
            using var stream = new MemoryStream();

            // Ensure that our stream is at the beginning.
            stream.Write(arrBytes, 0, arrBytes.Length);
            stream.Seek(0, SeekOrigin.Begin);

            return Serializer.Deserialize<T>(stream);
        }

        static async Task Main()
        {
            // The Service Bus client types are safe to cache and use as a singleton for the lifetime
            // of the application, which is best practice when messages are being published or read
            // regularly.
            //
            // Create the clients that we'll use for sending and processing messages.
            client = new ServiceBusClient(connectionString);

            // create a processor that we can use to process the messages
            processor = client.CreateProcessor(topicName, subscriptionName, new ServiceBusProcessorOptions { ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete });

            try
            {
                // add handler to process messages
                processor.ProcessMessageAsync += MessageHandler;

                // add handler to process any errors
                processor.ProcessErrorAsync += ErrorHandler;

                // start processing 
                await processor.StartProcessingAsync();

                Console.WriteLine("Wait for a minute and then press any key to end the processing");
                Console.ReadKey();

                // stop processing 
                Console.WriteLine("\nStopping the receiver...");
                await processor.StopProcessingAsync();
                Console.WriteLine("Stopped receiving messages");
            }
            finally
            {
                // Calling DisposeAsync on client types is required to ensure that network
                // resources and other unmanaged objects are properly cleaned up.
                await processor.DisposeAsync();
                await client.DisposeAsync();
            }
        }
    }
}