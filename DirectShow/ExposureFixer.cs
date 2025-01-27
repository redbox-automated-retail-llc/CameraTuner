using Redbox.HAL.Component.Model;

namespace Redbox.DirectShow
{
    public static class ExposureFixer
    {
        public static void ResetCameraProperties()
        {
            LogHelper.Instance.Log("Checking camera properties...");
            IDeviceDescriptor activeCamera = ServiceLocator.Instance.GetService<IUsbDeviceService>().FindActiveCamera(false);
            if (activeCamera == null)
            {
                LogHelper.Instance.Log("Unable to find an active camera.");
            }
            else
            {
                LogHelper.Instance.Log("Found camera device {0} ( {1} )", (object)activeCamera.ToString(), (object)activeCamera.Friendlyname);
                if (!activeCamera.Friendlyname.Equals("4th Gen"))
                    return;
                FilterInfoCollection filterInfoCollection = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (filterInfoCollection == null || filterInfoCollection.Count == 0)
                {
                    LogHelper.Instance.Log("Unable to find any devices.");
                }
                else
                {
                    PlayerDevice device = new PlayerDevice(filterInfoCollection[0].MonikerString, 500, false);
                    if (device == null)
                    {
                        LogHelper.Instance.Log("Found no devices.");
                    }
                    else
                    {
                        ExposureFixer.CameraProperties properties = ExposureFixer.GetProperties(device);
                        if (properties == null)
                            return;
                        if (CameraControlFlags.Auto != properties.Flags)
                            LogHelper.Instance.Log("Device is not set to auto.");
                        else if (!device.SetCameraProperty(CameraControlProperty.Exposure, properties.Value, CameraControlFlags.Manual))
                            LogHelper.Instance.Log(" !!Failed to set camera property to manual!!");
                        else
                            ExposureFixer.GetProperties(device);
                    }
                }
            }
        }

        private static ExposureFixer.CameraProperties GetProperties(PlayerDevice device)
        {
            int f = 0;
            CameraControlFlags controlFlags;
            if (!device.GetCameraProperty(CameraControlProperty.Exposure, out f, out controlFlags))
            {
                LogHelper.Instance.Log("Unable to retrieve camera properties.");
                return (ExposureFixer.CameraProperties)null;
            }
            LogHelper.Instance.Log(" GetProperties: Value = {0} Flags = {1}", (object)f, (object)controlFlags);
            return new ExposureFixer.CameraProperties(f, controlFlags);
        }

        private class CameraProperties
        {
            internal int Value { get; private set; }

            internal CameraControlFlags Flags { get; private set; }

            internal CameraProperties(int f, CameraControlFlags ccf)
            {
                this.Value = f;
                this.Flags = ccf;
            }
        }
    }
}
