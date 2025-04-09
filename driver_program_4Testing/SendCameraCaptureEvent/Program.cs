using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SendCameraCaptureEvent
{
    class Program
    {
        // connection string to your Service Bus namespace
        static string connectionString = "Endpoint=sb://xyzzzzz.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=YOUR_KEY";

        // name of your Service Bus topic
        static string topicName = "camera-request";

        // the client that owns the connection and can be used to create senders and receivers
        static ServiceBusClient client;

        // the sender used to publish messages to the topic
        static ServiceBusSender sender;

        // the processor that reads and processes messages from the subscription
        static ServiceBusProcessor processor;

        // name of the subscription to the topic
        static string subscriptionName = Environment.MachineName;

        // number of messages to be sent to the topic
        private const int numOfMessages = 1;

        /// <summary>
        /// Convert an object to a Byte Array, using Protobuf.
        /// </summary>
        public static byte[] ObjectToByteArray(Data obj)
        {
            if (obj == null)
                return null;

            using var stream = new MemoryStream();
            Serializer.Serialize(stream, obj);
            return stream.ToArray();
        }

        // handle received messages
        static async Task MessageHandler(ProcessMessageEventArgs args)
        {
            DeviceEvent dt = JsonConvert.DeserializeObject<DeviceEvent>(args.Message.Body.ToString());

            Console.WriteLine($"Received command: {dt.message} from subscription: {subscriptionName}");
            Console.WriteLine($"Received dir: {dt.result} from subscription: {subscriptionName}");

            // complete the message. messages is deleted from the subscription. 
            await args.CompleteMessageAsync(args.Message);
        }

        // handle any errors when receiving messages
        static Task ErrorHandler(ProcessErrorEventArgs args)
        {
            Console.WriteLine(args.Exception.ToString());
            return Task.CompletedTask;
        }

        static async Task Main(string[] args)
        {
            // The Service Bus client types are safe to cache and use as a singleton for the lifetime
            // of the application, which is best practice when messages are being published or read
            // regularly.
            //
            client = new ServiceBusClient(connectionString);
            sender = client.CreateSender(topicName);

            CameraEvent ce = new CameraEvent(CameraEventType.Start, @"abc/photos/RickyMartin-xyz-04082024");
            //CameraEvent ce = new CameraEvent(CameraEventType.Stop, "NA");

            //dt.command = "START";
            //dt.dir = Environment.CurrentDirectory;

            //ReadOnlyMemory<Byte> _rom = new ReadOnlyMemory<Byte>(ObjectToByteArray(dt));

            // create a batch 
            using ServiceBusMessageBatch messageBatch = await sender.CreateMessageBatchAsync();

            //for (int i = 1; i <= numOfMessages; i++)
            {
                // try adding a message to the batch
                //if (!messageBatch.TryAddMessage(new ServiceBusMessage($"Message {i}")))
                if (!messageBatch.TryAddMessage(new ServiceBusMessage(JsonConvert.SerializeObject(ce))))
                {
                    // if it is too large for the batch
                    throw new Exception($"The message 1 is too large to fit in the batch.");
                }
            }

            try
            {
                // Use the producer client to send the batch of messages to the Service Bus topic
                await sender.SendMessagesAsync(messageBatch);
                Console.WriteLine($"A batch of {numOfMessages} messages has been published to the topic.");

                await sender.DisposeAsync();
                await client.DisposeAsync();

                //Thread.Sleep(2000);

                //client = new ServiceBusClient(connectionString);
                //ServiceBusProcessorOptions sbpo = new ServiceBusProcessorOptions();
                //sbpo.ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete;

                //// create a processor that we can use to process the messages
                //processor = client.CreateProcessor(topicName, subscriptionName, sbpo);

                //// add handler to process messages
                //processor.ProcessMessageAsync += MessageHandler;

                //// add handler to process any errors
                //processor.ProcessErrorAsync += ErrorHandler;

                //// start processing 
                //await processor.StartProcessingAsync();

                //Console.WriteLine("Wait for a minute and then press any key to end the processing");
                //Console.ReadKey();

                //// stop processing 
                //Console.WriteLine("\nStopping the receiver...");
                //await processor.StopProcessingAsync();
                //Console.WriteLine("Stopped receiving messages");

                //await processor.DisposeAsync();
                //await client.DisposeAsync();
            }
            finally
            {
                // Calling DisposeAsync on client types is required to ensure that network
                // resources and other unmanaged objects are properly cleaned up.
            }

            Console.WriteLine("Press any key to end the application");
            Console.ReadKey();
        }
    }
}
