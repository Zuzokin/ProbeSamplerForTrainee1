using System.Collections.ObjectModel;

namespace ProbeSampler.Core.Services.Contract
{
    /// <summary>
    /// Сервис для поиска подключенных к компьюетру камер.
    /// </summary>
    public interface ICameraLocator
    {
        /// <summary>
        /// Список камер.
        /// </summary>
        ReadOnlyCollection<CameraConnection> DeviceList { get; }
        /// <summary>
        /// Поиск камер.
        /// </summary>
        void SearchDevices();
    }
}
