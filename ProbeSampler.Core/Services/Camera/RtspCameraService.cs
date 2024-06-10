using Emgu.CV;

namespace ProbeSampler.Core.Services.Camera
{
    /// <summary>
    /// RTSP камера.
    /// </summary>
    public class RtspCameraService : BindableBase, ICamera
    {
        // По сути это обертка поверх Emgu.CV.VideoCapture
        // для более информативного отображения в WPF приложении.
        // Теоретически может поддерживать и видео файл, но я не пробовал.

        private object locker = new object();
        /// <summary>
        /// Сам класс Emgu.CV.VideoCapture. Основная логика захвата здесь.
        /// </summary>
        VideoCapture? capture;
        /// <summary>
        /// Время таймаут для подключения, внешнего API для настройки нет.
        /// </summary>
        readonly TimeSpan connectionTimeout;
        /// <summary>
        /// Внутренний источнк токена отмены.
        /// </summary>
        CancellationTokenSource? internalCts;
        /// <summary>
        /// Таймер задержки.
        /// </summary>
        readonly Timer? retrievalTimer;
        /// <summary>
        /// Флажок запрета получения кадра.
        /// </summary>
        bool canRetrieve = true;

        private CameraState state;

        public CameraState State
        {
            get => state;
            private set => SetProperty(ref state, value);
        }

        public event EventHandler<FrameEventArgs>? FrameReceived;

        public RtspCameraService(int connectionTimeoutSec = 10)
        {
            connectionTimeout = TimeSpan.FromSeconds(connectionTimeoutSec);

            retrievalTimer = new Timer(
                state =>
            {
                canRetrieve = true;
                retrievalTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            }, null, Timeout.Infinite, Timeout.Infinite);
        }

        public async Task ConnectAsync(string url, CancellationToken externalToken = default)
        {
            // Здесь внутренний и внешний токены объединяются, чтобы обрабатывать и внутреннюю отмену и внешнюю.
            internalCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var ct = internalCts.Token;
            State = CameraState.Connecting;
            try
            {
                // Таким образом проверяется доступность источника
                var probeConnectionTask = Task.Run(
                    () =>
                {
                    VideoCapture? probeCapture = null;
                    try
                    {
                        ct.ThrowIfCancellationRequested();
                        probeCapture = new VideoCapture(url);
                    }
                    finally
                    {
                        probeCapture?.Stop();
                        probeCapture?.Dispose();
                    }
                }, ct);

                var timeOutTask = Task.Delay(connectionTimeout, ct);

                ct.ThrowIfCancellationRequested();

                // Принцип следующий: если вернется задача подключения, значит источник доступен,
                // если задача ожидания, значит источник недоступен.
                var probeTask = await Task.WhenAny(probeConnectionTask, timeOutTask);

                if (probeTask == timeOutTask)
                {
                    ct.ThrowIfCancellationRequested();
                    State = CameraState.Error;
                    throw new TimeoutException();
                }

                ct.ThrowIfCancellationRequested();
                // Соотвественно если мы дошли до сюда, то можно
                // создать настоящие подключение к источнику
                await Task.Run(
                    () =>
                {
                    ct.ThrowIfCancellationRequested();
                    capture = new VideoCapture(url);
                    capture.ImageGrabbed += Capture_ImageGrabbed;
                    capture.Start();
                }, ct);

                ct.ThrowIfCancellationRequested();

                State = CameraState.Running;
            }
            catch (OperationCanceledException ex)
            {
                capture?.Dispose();
                State = CameraState.Stopped;
                throw ex;
            }
            catch (Exception)
            {
                capture?.Dispose();
                State = CameraState.Error;
                throw;
            }
        }

        public void Start()
        {
            try
            {
                capture?.Start();
                State = CameraState.Running;
            }
            catch (Exception)
            {
                State = CameraState.Error;
                throw;
            }
        }

        public void Pause()
        {
            try
            {
                capture?.Pause();
                State = CameraState.Pause;
            }
            catch (Exception)
            {
                State = CameraState.Error;
                throw;
            }
        }

        public void Stop()
        {
            try
            {
                State = CameraState.Stopping;
                capture?.Stop();
                State = CameraState.Stopped;
            }
            catch (Exception)
            {
                State = CameraState.Error;
                throw;
            }
        }

        private void Capture_ImageGrabbed(object? sender, EventArgs e)
        {
            if (canRetrieve && capture != null)
            {
                canRetrieve = false;
                // Таймер существует для ограничения получения кадров,
                // иначе будет слишком большое использование ресурсов.
                // Извне изменить время задержки нельзя
                retrievalTimer?.Change(30, Timeout.Infinite);

                var frame = new Mat();
                capture.Retrieve(frame);
                FrameReceived?.Invoke(this, new FrameEventArgs(frame));
            }
        }

        public void Dispose()
        {
            lock (locker)
            {
                if (capture == null)
                {
                    return;
                }

                internalCts?.Cancel();
                retrievalTimer?.Dispose();

                capture.ImageGrabbed -= Capture_ImageGrabbed;
                capture.Stop();
                capture.Dispose();
                capture = null;
                canRetrieve = false;
            }
        }
    }
}
