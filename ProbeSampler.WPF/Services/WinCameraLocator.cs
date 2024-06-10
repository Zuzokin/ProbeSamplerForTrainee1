using DirectShowLib;

namespace ProbeSampler.WPF
{
    public class WinCameraLocator : ICameraLocator
    {
        private readonly List<CameraConnection> deviceList = new();

        public ReadOnlyCollection<CameraConnection> DeviceList { get; }

        public WinCameraLocator()
        {
            DeviceList = new ReadOnlyCollection<CameraConnection>(deviceList);
        }

        public void SearchDevices()
        {
            deviceList.Clear();

            var cams = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice)
                .Select(v => new CameraConnection()
                {
                    URL = v.DevicePath,
                });
            deviceList.AddRange(cams);
        }

        private void GetIpCameras()
        {
            throw new NotImplementedException();
        }
    }
}
