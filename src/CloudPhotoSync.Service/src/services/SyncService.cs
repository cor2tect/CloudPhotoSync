using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CloudPhotoSync.Service.model;
using CloudPhotoSync.Service.util;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CloudPhotoSync.Service.services
{
    public class SyncService : BackgroundService
    {
        private readonly ILogger<SyncService> logger;
        private readonly SyncServiceOptions options;
        private readonly BlobObjectStoreFactory blobObjectStoreFactory;
        private readonly ServiceBusFactory serviceBusFactory;
        private readonly CameraController cameraController;

        public SyncService(
            ILogger<SyncService> logger,
            SyncServiceOptions options,
            ServiceBusFactory serviceBusFactory,
            BlobObjectStoreFactory blobObjectStoreFactory,
            CameraController cameraController
        )
        {
            this.logger = logger;
            this.options = options;
            this.serviceBusFactory = serviceBusFactory;
            this.blobObjectStoreFactory = blobObjectStoreFactory;
            this.cameraController = cameraController;
        }

        private async Task ExecuteInternalAsync(CancellationToken ct)
        { 
            logger.LogInformation("Init Camera Event Receiver");

            var cameraEventReceiver = await serviceBusFactory
                .GetEventReceiverAsync<CameraEvent>(
                    requestTopicName: options.RequestTopic,
                    subscriptionName: options.Subscription
                );
            
            var deviceEventSender = await serviceBusFactory
                .GetEventSender<DeviceEvent>(
                    responseTopicName: options.ResponseTopic,
                    subscriptionName: options.Subscription
                );

            cameraController.SetServiceBusSender(deviceEventSender);

            logger.LogInformation("Listening for events...");

            static async Task InitStream<T>(
                IObservable<T> source,
                Func<T, Task> handler
            ) => await source
                .Select(handler)
                .Select(t => t.ToObservable())
                .Concat()
                .LastAsync();

            await Task.WhenAll(
                InitStream(
                    source: cameraEventReceiver.GetEventStream(),
                    handler: CameraManagerAsync
                )
            );
            
            Task CameraManagerAsync(CameraEvent ce)
            {
                return ce.Command switch
                {
                    CameraEventType.Start => DoCameraConnect(ce.BlobPath),
                    CameraEventType.Stop => DoCameraDisconnect(),
                    _ => Task.CompletedTask
                };
            }

            Task DoCameraConnect(string blobPath)
            {
                logger.LogInformation("CameraEvent received: CameraEventType.Start");
                logger.LogInformation("BlobPath: " + blobPath);
                ConnectCamera(blobPath);
                return Task.CompletedTask;
            }

            Task DoCameraDisconnect()
            {
                logger.LogInformation("CameraEvent received: CameraEventType.Stop");
                DisconnectCamera();
                return Task.CompletedTask;
            }
        }

        private Task ConnectCamera(string blobPath)
        {
            cameraController.SetUploadingBlobPath(blobPath);
            cameraController.SetBlobObjectStore(blobObjectStoreFactory
                .GetObjectStore(
                    containerName: options.Container
                ));

            //if (cameraController.IsCameraConnected() == false)
            {
                cameraController.ConnectCamera();
            }
            
            return Task.CompletedTask;
        }

        private Task DisconnectCamera()
        {
            cameraController.DisconnectCamera();
            return Task.CompletedTask;
        }
        
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            try
            {
                logger.LogInformation("Started");

                await ExecuteInternalAsync(ct);

                logger.LogInformation("Completed");
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
                throw;
            }
        }
    }
}
