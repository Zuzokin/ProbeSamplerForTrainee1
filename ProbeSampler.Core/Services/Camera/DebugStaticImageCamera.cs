using Emgu.CV;

namespace ProbeSampler.Core.Services.Camera
{
    /// <summary>
    /// Для дебага распознования. Выводит статическую картинку
    /// </summary>
    public class DebugStaticImageCamera : BindableBase, ICamera
    {
        private Mat frame;
        private CancellationTokenSource? internalCts;
        private Timer? retrievalTimer;
        private readonly object locker = new();
        private string? filePath;

        private CameraState state;
        public CameraState State
        {
            get => state;
            private set => SetProperty(ref state, value);
        }

        public event EventHandler<FrameEventArgs>? FrameReceived;

        public DebugStaticImageCamera()
        {
            frame = new Mat();
            retrievalTimer = new Timer(state =>
            {
                EmitFrame();
            }, null, Timeout.Infinite, Timeout.Infinite);
        }

        private void EmitFrame()
        {
            lock (locker)
            {
                FrameReceived?.Invoke(this, new FrameEventArgs(frame.Clone()));
                retrievalTimer?.Change(30, Timeout.Infinite);
            }
        }

        public async Task ConnectAsync(string url, CancellationToken externalToken = default)
        {
            State = CameraState.Connecting;
            internalCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
            var ct = internalCts.Token;

            try
            {
                var probeConnectionTask = Task.Run(async () =>
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Delay(1000, ct);

                    if (!File.Exists(url))
                        throw new FileNotFoundException($"File {url} does not exist.");

                    filePath = url;
                    frame = CvInvoke.Imread(filePath);

                }, ct);

                await probeConnectionTask;
                retrievalTimer?.Change(0, 30);
                State = CameraState.Running;
            }
            catch (OperationCanceledException ex)
            {
                State = CameraState.Stopped;
                throw ex;
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
                retrievalTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                State = CameraState.Pause;
            }
            catch (Exception)
            {
                State = CameraState.Error;
                throw;
            }
        }

        public void Start()
        {
            try
            {
                retrievalTimer?.Change(0, 30);
                State = CameraState.Running;
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
                retrievalTimer?.Change(Timeout.Infinite, Timeout.Infinite);
                State = CameraState.Stopped;
            }
            catch (Exception)
            {
                State = CameraState.Error;
                throw;
            }

        }

        public void Dispose()
        {
            retrievalTimer?.Dispose();
            retrievalTimer = null;
            frame?.Dispose();
            internalCts?.Dispose();
        }
    }
}
