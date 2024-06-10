using ProbeSampler.Core.Services.Camera;

namespace ProbeSampler.Core.Services.Contract
{
    /// <summary>
    /// Сервис для отображения потокового изображения из заданного источника.
    /// </summary>
    public interface ICamera : IDisposable
    {
        /// <summary>
        /// Текущие состояние камеры.
        /// </summary>
        CameraState State { get; }
        /// <summary>
        /// Событие получения захваченного кадра
        /// </summary>
        event EventHandler<FrameEventArgs>? FrameReceived;
        /// <summary>
        /// Подключиться к камере.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="externalToken"></param>
        /// <returns></returns>
        Task ConnectAsync(string url, CancellationToken externalToken = default);
        /// <summary>
        /// Возобновить работу камеры из приостановленного состояния.
        /// </summary>
        /// <returns></returns>
        void Start();
        /// <summary>
        /// Приостановить работу камеры.
        /// </summary>
        /// <returns></returns>
        void Pause();
        /// <summary>
        /// Остановить работу камеры.
        /// </summary>
        /// <returns></returns>
        void Stop();
    }
}
