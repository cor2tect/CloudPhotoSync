/******************************************************************************
*                                                                             *
*   Description:                                                              *
*                                                                             *
*******************************************************************************
*                                                                             *
*   Written and developed by Sharif Bhuiyan                                   *
*   info@cor2tect.com, 2022                                           *
*                                                                             *
*******************************************************************************/

using System;
using System.Runtime.InteropServices;
using System.IO;

namespace CameraControl
{
    public class CameraManager : IObserver
    {
        GCHandle handle;
        IntPtr camera = IntPtr.Zero;
        bool isSDKLoaded = false;
        CameraController controller = null;
        CameraModel model = null;

        EDSDKLib.EDSDK.EdsPropertyEventHandler handlePropertyEvent = null;
        EDSDKLib.EDSDK.EdsObjectEventHandler handleObjectEvent = null;
        EDSDKLib.EDSDK.EdsStateEventHandler handleStateEvent = null;

        public event EventHandler OnConnect;
        public event EventHandler OnDisconnect;
        public event EventHandler OnReceiveImageData;

        /// <summary>
        /// </summary>
        //[STAThread]
        public uint ConnectToCamera()
        {
            // Initialization of SDK
            uint err = EDSDKLib.EDSDK.EdsInitializeSDK();

            isSDKLoaded = false;
            if (err == EDSDKLib.EDSDK.EDS_ERR_OK)
            {
                isSDKLoaded = true;
            }

            //Acquisition of camera list
            IntPtr cameraList = IntPtr.Zero;
            if (err == EDSDKLib.EDSDK.EDS_ERR_OK)
            {
                err = EDSDKLib.EDSDK.EdsGetCameraList(out cameraList);
            }

            //Acquisition of number of Cameras
            if (err == EDSDKLib.EDSDK.EDS_ERR_OK)
            {
                int count = 0;
                err = EDSDKLib.EDSDK.EdsGetChildCount(cameraList, out count);
                if (count == 0)
                {
                    err = EDSDKLib.EDSDK.EDS_ERR_DEVICE_NOT_FOUND;
                    return err;
                }
            }

            //Acquisition of camera at the head of the list
            camera = IntPtr.Zero;
            if (err == EDSDKLib.EDSDK.EDS_ERR_OK)
            {
                err = EDSDKLib.EDSDK.EdsGetChildAtIndex(cameraList, 0, out camera);
            }

            //Acquisition of camera information
            EDSDKLib.EDSDK.EdsDeviceInfo deviceInfo;
            if (err == EDSDKLib.EDSDK.EDS_ERR_OK)
            {
                err = EDSDKLib.EDSDK.EdsGetDeviceInfo(camera, out deviceInfo);
                if (err == EDSDKLib.EDSDK.EDS_ERR_OK && camera == IntPtr.Zero)
                {
                    err = EDSDKLib.EDSDK.EDS_ERR_DEVICE_NOT_FOUND;
                    return err;
                }
            }

            //Release camera list
            if (cameraList != IntPtr.Zero)
            {
                EDSDKLib.EDSDK.EdsRelease(cameraList);
            }

            //Create Camera model
            if (err == EDSDKLib.EDSDK.EDS_ERR_OK)
            {
                model = new CameraModel(camera);
            }

            if (err != EDSDKLib.EDSDK.EDS_ERR_OK)
            {
                return err;
            }

            Utility.g_ms = new MemoryStream();

            handlePropertyEvent = new EDSDKLib.EDSDK.EdsPropertyEventHandler(CameraEventListener.HandlePropertyEvent);
            handleObjectEvent = new EDSDKLib.EDSDK.EdsObjectEventHandler(CameraEventListener.HandleObjectEvent);
            handleStateEvent = new EDSDKLib.EDSDK.EdsStateEventHandler(CameraEventListener.HandleStateEvent);
           
            if (err == EDSDKLib.EDSDK.EDS_ERR_OK)
            {
                //Create CameraController
                controller = new CameraController(ref model);
                handle = GCHandle.Alloc(controller);
                IntPtr ptr = GCHandle.ToIntPtr(handle);

                //Set Property Event Handler
                if (err == EDSDKLib.EDSDK.EDS_ERR_OK)
                {
                    err = EDSDKLib.EDSDK.EdsSetPropertyEventHandler(camera, EDSDKLib.EDSDK.PropertyEvent_All, handlePropertyEvent, ptr);
                }

                //Set Object Event Handler
                if (err == EDSDKLib.EDSDK.EDS_ERR_OK)
                {
                    err = EDSDKLib.EDSDK.EdsSetObjectEventHandler(camera, EDSDKLib.EDSDK.ObjectEvent_All, handleObjectEvent, ptr);
                }

                //Set State Event Handler
                if (err == EDSDKLib.EDSDK.EDS_ERR_OK)
                {
                    err = EDSDKLib.EDSDK.EdsSetCameraStateEventHandler(camera, EDSDKLib.EDSDK.StateEvent_All, handleStateEvent, ptr);
                }

                IObserver _obs = (IObserver)this;
                model.Add(ref _obs);

                controller.Run();

                if (OnConnect != null)
                {
                    OnConnect(this, EventArgs.Empty);
                }
            }

            return EDSDKLib.EDSDK.EDS_ERR_OK;
        }

        public uint DisconnectCamera()
        {
            //Termination of SDK
            if (isSDKLoaded)
            {
                if (model != null)
                {
                    handle.Free();
                    model = null;
                }

                GC.KeepAlive(handlePropertyEvent);
                GC.KeepAlive(handleObjectEvent);
                GC.KeepAlive(handleStateEvent);

                //Release Camera
                if (camera != IntPtr.Zero)
                {
                    EDSDKLib.EDSDK.EdsRelease(camera);
                }

                EDSDKLib.EDSDK.EdsTerminateSDK();
                isSDKLoaded = false;

                if(Utility.g_ms != null) Utility.g_ms.Close();

                if (OnDisconnect != null)
                {
                    OnDisconnect(this, EventArgs.Empty);
                }
            }

            return 0;
        }

        public void Update(Observable observable, CameraEvent e)
        {
            if (e.GetEventType() == CameraEvent.Type.DOWNLOAD_COMPLETE)
            {
                if (OnReceiveImageData != null)
                {
                    OnReceiveImageData(Utility.g_ms, EventArgs.Empty);
                }
            }
            else if(e.GetEventType() == CameraEvent.Type.SHUT_DOWN)
            {
                if(OnDisconnect != null)
                {
                    OnDisconnect(CameraEvent.Type.SHUT_DOWN.ToString(), EventArgs.Empty);
                }
            }
        }
    }
}
