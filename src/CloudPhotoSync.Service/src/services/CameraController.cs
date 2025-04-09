//
// ***************************CameraController***************************************
// Date: 05/02/2022
// Written By Sharif Bhuiyan
// info@cor2tect.com
//
// Description:
// Controlling Canon DSLR Camera via EDSDK low level library.
// It handles camera connect, external capture, disconnect & threading
//
// You can use it freely and
// feel free to modify according to your software design/pattern
// ********************************END**********************************
//

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using CloudPhotoSync.Service.model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static CloudPhotoSync.Service.src.util.Win32Utility;

namespace CloudPhotoSync.Service.services
{
    public class CameraController
    {
        private bool cameraInitialized = false;
        private readonly CameraControl.CameraManager cameraControlX1;
        private string blobPath = string.Empty;
        private Thread connectionThread;
        private readonly AutoResetEvent connectionThreadRunning = new AutoResetEvent(false);
        private readonly ILogger<CameraController> logger;
        private BlobObjectStore blobStore;
        private ServiceBusSender deviceEventMsgSender;
        private readonly SyncServiceOptions syncServiceOptions;

        public CameraController(ILogger<CameraController> logger, SyncServiceOptions syncServiceOptions)
        {
            this.logger = logger;
            this.syncServiceOptions = syncServiceOptions;

            cameraControlX1 = new CameraControl.CameraManager();

            try
            {
                cameraControlX1.OnConnect += CameraControlX1_OnConnect;
                cameraControlX1.OnDisconnect += CameraControlX1_OnDisconnect;
                cameraControlX1.OnReceiveImageData += CameraControlX1_OnReceiveImageData;
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to register camera event handlers. Reason: {ex.Message}");
            }
        }

        public bool IsCameraConnected()
        {
            return this.cameraInitialized;
        }

        public void SetUploadingBlobPath(string blobPath)
        {
            this.blobPath = blobPath;
        }

        public void SetBlobObjectStore(BlobObjectStore blobStore)
        {
            this.blobStore = blobStore;
        }

        public void SetServiceBusSender(ServiceBusSender sender)
        {
            this.deviceEventMsgSender = sender;
        }

        public void ConnectCamera()
        {
            logger.LogDebug("Connecting to camera...");
            connectionThreadRunning.Reset();
            StartCameraThread();
        }

        private void StartCameraThread()
        {
            connectionThread = new Thread(RunCameraConnectionThread) { Name = "CameraSessionRunning" };
            connectionThread.Start();
            connectionThreadRunning.WaitOne();
        }
        private void RunCameraConnectionThread()
        {
            ConnectToCamera();
            connectionThreadRunning.Set();
            var msg = new NativeMethods.MSG();
            while (true)
            {
                if (NativeMethods.PeekMessage(out msg, IntPtr.Zero, 0, 0, NativeMethods.PM_REMOVE))
                {
                    NativeMethods.TranslateMessage(ref msg);
                    NativeMethods.DispatchMessage(ref msg);
                }

                if (cameraInitialized == false) break;
            }
        }

        private async void ConnectToCamera()
        {
            DeviceEvent? deviceEvent = null;
            uint res = cameraControlX1.ConnectToCamera();

            if (res == EDSDKLib.EDSDK.EDS_ERR_DEVICE_NOT_FOUND)
            {
                cameraControlX1.DisconnectCamera();
            }
            else if (res == EDSDKLib.EDSDK.EDS_ERR_OK){}
            else
            {
                logger.LogWarning("Unknown Camera error!");
                deviceEvent = new DeviceEvent(DeviceEventType.MiscError, "These was an error connecting to the Camera. Please try restarting the Windows Service and connecting again.");
                logger.LogDebug("Sending 'unknown camera connection error' device event ...");
                await SendDeviceEvent(deviceEvent);
            }
        }

        private async Task SendDeviceEvent(DeviceEvent deviceEvent)
        {
            // Send the message if we have a sender
            if (deviceEventMsgSender != null)
            {
                var eventName = deviceEvent.GetType().Name;
                var jsonMessage = JsonConvert.SerializeObject(deviceEvent);
                var body = Encoding.UTF8.GetBytes(jsonMessage);

                var message = new ServiceBusMessage
                {
                    MessageId = Guid.NewGuid().ToString(),
                    Subject = eventName,
                    Body = new BinaryData(body)
                };
                try
                {
                    await deviceEventMsgSender.SendMessageAsync(message);
                }
                catch (Exception ex)
                {
                    logger.LogError("Error sending device event message.", ex);
                    throw;
                }
            }
            else
            {
                logger.LogWarning("Device event sender not initialized.");
            }
        }

        public void DisconnectCamera()
        {
            uint res = cameraControlX1.DisconnectCamera();
        }

        private async void CameraControlX1_OnDisconnect(object? sender, EventArgs e)
        {
            DeviceEvent? deviceEvent = null;
            
            //handling shutdown = self-healing for the cam device session without restarting the service
            if (sender is string)
            {
                if (sender.ToString() == "SHUT_DOWN")
                {
                    logger.LogInformation("Camera disconnected automatically or accidentally!");
                    deviceEvent = new DeviceEvent(DeviceEventType.DeviceShutDownAccidentally, "Please send the STOP request first, otherwise camera SDK will not be initiated for this session until you restart the windows service process.");

                    logger.LogDebug("Sending 'Device ShutDown Accidentally' device event ...");
                    await SendDeviceEvent(deviceEvent);
                    return;
                }
            }

            logger.LogInformation("Released camera SDK resources.");
            this.blobPath = ""; 

            if (cameraInitialized == true)
            {
                cameraInitialized = false;
                logger.LogInformation("Camera disconnected!");
                deviceEvent = new DeviceEvent(DeviceEventType.Ok, "Camera disconnected successfully.");

                logger.LogDebug("Sending 'camera disconnected' device event ...");
                await SendDeviceEvent(deviceEvent);
            }
            else
            {
                logger.LogWarning("Camera not found!");
                deviceEvent = new DeviceEvent(DeviceEventType.DeviceNotFound, "The Camera wasn't found. Please connect the camera, turn it on, and try connecting again.");

                logger.LogDebug("Sending 'camera not found' device event ...");
                await SendDeviceEvent(deviceEvent);
            }
        }

        private async void CameraControlX1_OnConnect(object? sender, EventArgs e)
        {
            cameraInitialized = true;
            DeviceEvent? deviceEvent = null;

            logger.LogInformation("Camera connected!");
            deviceEvent = new DeviceEvent(DeviceEventType.Ok, "Camera connected successfully.");
            
            logger.LogDebug("Sending 'camera connected' device event ...");
            await SendDeviceEvent(deviceEvent);
        }

        private async void CameraControlX1_OnReceiveImageData(object? sender, EventArgs e)
        {
            // Only process images if the blob path has been set
            if (String.IsNullOrEmpty(this.blobPath)) return;

            if (sender == null) { logger.LogWarning("Patient photo data is empty."); return; }

            MemoryStream imageData = (MemoryStream)sender;
            imageData.Position = 0;
            logger.LogInformation("Patient photo data received!");

            var filePath = syncServiceOptions.SyncPrefix + blobPath + syncServiceOptions.SyncFolder +
                           DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + CameraControl.Utility.g_fileExtension.ToLower();

            var byteTransferredSize = await blobStore.WriteAsync(filePath, imageData);

            if (byteTransferredSize > 0)
            {
                logger.LogDebug($"Uploaded Blob file: {filePath}");
                logger.LogDebug($"Bytes transferred: {byteTransferredSize:n0}");
            }
        }
    }
}
