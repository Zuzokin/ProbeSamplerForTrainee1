using Emgu.CV;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace ProbeSampler.Presentation
{
    public class ConfigEditorViewModel : RoutableViewModelBase
    {
        #region TabsVMs

        [Reactive] public CameraConnectionEditorViewModel CameraConnectionVM { get; set; }

        [Reactive] public GridOverlayEditorViewModel GridOverlayVM { get; set; }

        [Reactive] public SamplerConnectionEditorViewModel SamplerConnectionVM { get; set; }

        #endregion

        private CameraViewModel cameraVM;
        private IConnectionConfigurationManager manager;
        private ImageRotationProcessor? imageRotationProcessor;
        private UndistortProcessor? undistortProcessor;

        [ObservableAsProperty] public bool CanCamOpera { get; }

        [ObservableAsProperty] public bool CameraBusy { get; }

        public ReactiveCommand<Unit, Unit>? StartCommand { get; set; }

        public ReactiveCommand<Unit, Unit>? StopCommand { get; set; }

        public ReactiveCommand<Unit, Unit>? ConnectCommand { get; set; }

        public ReactiveCommand<Unit, Unit>? CancelConnectCommand { get; set; }

        public ReactiveCommand<Unit, Unit>? PauseCommand { get; set; }

        public ReactiveCommand<Unit, Unit>? SaveCommand { get; set; }

        public ReactiveCommand<Unit, Unit>? CancelCommand { get; set; }

        [Reactive] public string? StringPathToCalibrateFiles { get; set; }

        #region CameraConnectionConfig

        [Reactive] private ConnectionConfiguration? ConnectionConfiguration { get; set; }

        #endregion

        public ConfigEditorViewModel(Guid? configID = null)
            : base()
        {
            cameraVM = resolver.GetRequiredService<CameraViewModel>();
            cameraVM.ImageProcessingPipeline += CameraVM_ImageProcessingPipeline;
            manager = resolver.GetRequiredService<IConnectionConfigurationManager>();

            if (configID != null)
            {
                FindConfig((Guid)configID);
            }
            else
            {
                CreateNewConfig();
            }

            CameraConnectionVM = new(cameraVM, ConnectionConfiguration ?? new());
            GridOverlayVM = new(cameraVM, ConnectionConfiguration ?? new());
            SamplerConnectionVM = new(connectionConfiguration: ConnectionConfiguration ?? new());
        }

        private Mat CameraVM_ImageProcessingPipeline(Mat input)
        {
            Mat output = new Mat();
            if (imageRotationProcessor != null)
            {
                output = imageRotationProcessor.ReceiveImage(input);
            }

            if (undistortProcessor != null)
            {
                output = undistortProcessor.ReceiveImage(output.IsEmpty ? input : output);
            }

            return output;
        }

        protected override void SetupSubscriptions(CompositeDisposable d)
        {
            base.SetupSubscriptions(d);

            imageRotationProcessor = new ImageRotationProcessor()
                .DisposeWith(d);
            undistortProcessor = new UndistortProcessor()
                .DisposeWith(d);

            this.WhenAnyValue(x => x.ConnectionConfiguration)
                .Where(x => x != null)
                .Subscribe(connection =>
                {
                    if (connection != null)
                    {
                        CameraConnectionVM.ConnectionConfiguration = connection;
                        GridOverlayVM.ConnectionConfiguration = connection;
                        undistortProcessor.DistCoeffsArray = connection.CameraConnection.DistCoeff;
                        undistortProcessor.CameraMatrixArray = connection.CameraConnection.CameraMatrix;
                        undistortProcessor.Alpha = connection.CameraConnection.Alpha;
                        imageRotationProcessor.Angle = connection.CameraConnection.RotationAngle;
                    }

                    /*                    connection.WhenAnyValue(x => x.CameraConnection)
                                            .Where(x => x != null)
                                            .Subscribe(camConnection =>
                                            {
                                                camConnection.WhenAnyValue(x => x.DistCoeff)
                                                    .Where(x => x != null)
                                                    .Subscribe(dist =>
                                                    {
                                                        undistortProcessor.DistCoeffsArray = dist;
                                                    })
                                                    .DisposeWith(d);
                                                camConnection.WhenAnyValue(x => x.CameraMatrix)
                                                    .Where(x => x != null)
                                                    .Subscribe(matrix =>
                                                    {
                                                        undistortProcessor.CameraMatrixArray = matrix;
                                                    })
                                                    .DisposeWith(d);
                                                camConnection.WhenAnyValue(x => x.Alpha)
                                                    .Where(x => x > 0 && x < 1)
                                                    .Subscribe(alpha =>
                                                    {
                                                        undistortProcessor.Alpha = alpha;
                                                    })
                                                    .DisposeWith(d);
                                                camConnection.WhenAnyValue(x => x.RotationAngle)
                                                    .Subscribe(angle =>
                                                    {
                                                        imageRotationProcessor.Angle = angle;
                                                    })
                                                    .DisposeWith(d);
                                            })
                                            .DisposeWith(d);*/
                })
                .DisposeWith(d);

            CameraConnectionVM.WhenAnyValue(x => x.Name)
                .Select(name => name)
                .ToPropertyEx(this, x => x.ViewName)
                .DisposeWith(d);
        }

        protected override void SetupCommands()
        {
            base.SetupCommands();

            var cancelSubject = new Subject<Unit>();

            var canConnect = cameraVM.WhenAnyValue(x => x.CameraState)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x == CameraState.Stopped || x == CameraState.Error);

            ConnectCommand = ReactiveCommand.CreateFromObservable(
                () =>
                Observable.StartAsync(ct => cameraVM.Connect(ConnectionConfiguration?.CameraConnection?.URL ?? string.Empty, ct))
                .TakeUntil(cancelSubject), canConnect);

            var canStop = cameraVM.WhenAnyValue(x => x.CameraState)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x == CameraState.Running);

            StopCommand = ReactiveCommand.Create(cameraVM.Stop, canStop);

            var canStart = cameraVM.WhenAnyValue(x => x.CameraState)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x == CameraState.Pause);

            StartCommand = ReactiveCommand.Create(cameraVM.Start, canStart);

            var canPause = cameraVM.WhenAnyValue(x => x.CameraState)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x == CameraState.Running);

            PauseCommand = ReactiveCommand.Create(cameraVM.Pause, canPause);

            CancelConnectCommand = ReactiveCommand.Create(
                () => cancelSubject.OnNext(Unit.Default),
                ConnectCommand.IsExecuting);

            SaveCommand = ReactiveCommand.CreateFromTask(Save);

            CameraConnectionVM.CancelConnectCommand = CancelConnectCommand;

            CancelCommand = ReactiveCommand.Create(Cancel);
        }

        private void Cancel()
        {
            HostScreen.Router.NavigateBack?.Execute();
        }

        protected override void SetupValidation(CompositeDisposable d)
        {
            base.SetupValidation(d);
            /*
                        this.ValidationRule(
                            viewmodel => viewmodel.GridOverlayCellHeight,
                            step => step >= 10,
                            "Слишком маленькое значение")
                            .DisposeWith(d);*/
        }

        protected override void SetupDeactivate()
        {
            base.SetupDeactivate();

            /*            this.CancelStartCameraCommand?
                            .Execute().Subscribe();*/

            cameraVM.ImageProcessingPipeline -= CameraVM_ImageProcessingPipeline;
            cameraVM.Dispose();
        }

        private void CreateNewConfig()
        {
            ConnectionConfiguration = new ConnectionConfiguration
            {
                CameraConnection = new(),
                SamplerConnection = new(),
            };
        }

        private void FindConfig(Guid id)
        {
            var original = manager.Get(id);
            if (original == null)
            {
                return;
            }

            ConnectionConfiguration = original.Clone() as ConnectionConfiguration;
        }

        private async Task Save()
        {
            ConnectionConfiguration? backup = null;

            if (ConnectionConfiguration == null)
            {
                return;
            }

            var original = manager.Get(ConnectionConfiguration.Id);

            if (original != null)
            {
                backup = original.Clone() as ConnectionConfiguration;
            }

            var result = await manager.AddAsync(ConnectionConfiguration);

            if (!result)
            {
                if (backup != null)
                {
                    original = backup;
                }
            }
            else
            {
                HostScreen.Router.NavigateBack?.Execute();
            }
        }
    }
}
