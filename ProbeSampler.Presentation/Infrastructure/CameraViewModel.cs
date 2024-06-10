using Emgu.CV;
using ProbeSampler.Core.Services.Camera;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProbeSampler.Presentation
{
    public class CameraViewModel : ViewModelBase, INestedMessageSender, IDisposable
    {
        [Reactive] private ICamera? cameraService { get; set; }

        private bool? lastDebugState = null;
        private IPathService pathService;
        private readonly Func<bool, ICamera> cameraFactory;

        public delegate Mat ImageProcessingHandler(Mat input);

        public event ImageProcessingHandler? ImageProcessingPipeline;

        public ISBMessageSender? MessageSender { get; set; }

        [ObservableAsProperty] public CameraState CameraState { get; }

        [ObservableAsProperty] public Size FrameSize { get; }

        [Reactive] public object? Overlay { get; set; }

        [Reactive] public int FrameHeight { get; private set; }

        [Reactive] public int FrameWidth { get; private set; }

        [Reactive] public Mat? CapturedFrame { get; private set; }

        [Reactive] public bool IsDebug { get; set; }

        public CameraViewModel(Func<bool, ICamera> cameraFactory, IPathService pathService)
        {
            this.cameraFactory = cameraFactory;
            this.pathService = pathService;
        }

        private void OnFrameReceived(object? sender, FrameEventArgs e)
        {
            Mat processedFrame = e.Frame;
            FrameHeight = processedFrame.Height;
            FrameWidth = processedFrame.Width;
            if (ImageProcessingPipeline != null)
            {
                processedFrame = ImageProcessingPipeline.Invoke(e.Frame);
            }

            CapturedFrame = processedFrame;
        }

        protected override void SetupSubscriptions(CompositeDisposable d)
        {
            base.SetupSubscriptions(d);

            this.WhenAnyValue(x => x.cameraService)
                .Where(x => x != null)
                .Subscribe(camera =>
                {
                    camera.WhenAnyValue(x => x.State)
                        .ToPropertyEx(this, x => x.CameraState)
                        .DisposeWith(d);
                })
                .DisposeWith(d);
            this.WhenAnyValue(x => x.FrameWidth, x => x.FrameHeight)
                .Where(x => x.Item1 > 0 && x.Item2 > 0)
                .Select(x => new Size(x.Item1, x.Item2))
                .ToPropertyEx(this, x => x.FrameSize)
                .DisposeWith(d);
            this.WhenAnyValue(x => x.IsDebug)
                .Subscribe(x =>
                {
                    CreateCamera();
                })
                .DisposeWith(d);
        }

        protected override void SetupDeactivate()
        {
            base.SetupDeactivate();
        }

        #region CameraActions

        public async Task Connect(string url, CancellationToken ct = default)
        {
            if (cameraService == null)
            {
                return;
            }

            this.Log().Debug($"Connecting to url {url}");

            try
            {
                await cameraService.ConnectAsync(url, ct);
                this.Log().Info($"Camera connected to url {url}");
            }
            catch (Exception ex)
            {
                HandlCameraExceptions(ex);
            }
        }

        public void Start()
        {
            if (cameraService == null)
            {
                return;
            }

            try
            {
                cameraService.Start();
                this.Log().Info("Camera started");
            }
            catch (Exception ex)
            {
                HandlCameraExceptions(ex);
            }
        }

        public void Pause()
        {
            if (cameraService == null)
            {
                return;
            }

            try
            {
                cameraService.Pause();
                this.Log().Info("Camera paused");
            }
            catch (Exception ex)
            {
                HandlCameraExceptions(ex);
            }
        }

        public void Stop()
        {
            if (cameraService == null)
            {
                return;
            }

            try
            {
                cameraService.Stop();
                this.Log().Info("Camera stoped");
            }
            catch (Exception ex)
            {
                HandlCameraExceptions(ex);
            }
        }

        public async Task SaveFrame()
        {
            if (cameraService == null)
            {
                return;
            }

            await Task.Run(() =>
            {
                if (CapturedFrame != null && !CapturedFrame.IsEmpty)
                {
                    var path = Path.Combine(pathService.CapturesPath, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");
                    CvInvoke.Imwrite(path, CapturedFrame);
                    this.Log().Info($"Frame saved to {path}");
                }
                else
                {
                    this.Log().Warn("No frame available to save");
                }
            });
        }

        #endregion

        private void CreateCamera()
        {
            if (lastDebugState.HasValue && lastDebugState.Value == IsDebug)
            {
                return;
            }

            CameraState? previousCameraState = cameraService?.State;

            CloseCamera();
            cameraService = cameraFactory(IsDebug);
            cameraService.FrameReceived += OnFrameReceived;

            if (previousCameraState.HasValue && previousCameraState.Value == CameraState.Running)
            {
                Start();
            }

            lastDebugState = IsDebug;
        }

        private void CloseCamera()
        {
            if (cameraService != null)
            {
                cameraService.FrameReceived -= OnFrameReceived;
                Stop();
                cameraService.Dispose();
            }
        }

        private void HandlCameraExceptions(Exception ex)
        {
            switch (ex)
            {
                case OperationCanceledException operationCanceledException:
                    this.Log().Info(operationCanceledException);
                    this.SendMessageToBus("Соединение отменено");
                    break;
                case TimeoutException timeoutException:
                    this.Log().Error(timeoutException);
                    this.SendMessageToBus("Время ожидания ответа истекло");
                    break;
                case FileNotFoundException fileNotFoundException:
                    this.Log().Error(fileNotFoundException);
                    this.SendMessageToBus("Не удается открыть указанный файл");
                    break;
                case Exception exception:
                    this.Log().Error(exception);
                    this.SendMessageToBus("Не удалось подключиться из-за неизвестной ошибки");
                    break;
                default:
                    this.SendMessageToBus("Не удалось подключиться из-за неизвестной ошибки");
                    break;
            }
        }

        public void Dispose()
        {
            CloseCamera();
        }
    }
}
