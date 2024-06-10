using System.Reactive;

namespace ProbeSampler.Presentation
{
    public class CameraConnectionEditorViewModel : ViewModelBase
    {
        private readonly Interaction<Unit, string> loadFileConfirm = new();
        private readonly IConnectionConfigurationManager manager;

        [Reactive] public CameraViewModel CameraVM { get; private set; }

        [Reactive] public ConnectionConfiguration ConnectionConfiguration { get; set; }

        [ObservableAsProperty] public string CameraInputDimens { get; } = "";

        [ObservableAsProperty] public bool IsNewConfiguration { get; }

        [Reactive] public string? Name { get; set; } = "Новое подключение";

        [Reactive] public string? URL { get; set; }

        [Reactive] public bool IsDebug { get; set; }

        [Reactive] public int CameraInputHeight { get; private set; }

        [Reactive] public int CameraInputWidth { get; private set; }

        public Interaction<Unit, string> LoadFileConfirm => loadFileConfirm;

        public ReactiveCommand<Unit, Unit>? CancelConnectCommand { get; set; }

        public ReactiveCommand<Unit, Unit>? OpenCalibrationViewCommand { get; set; }

        public ReactiveCommand<Unit, Unit>? SelectImageFileCommand { get; private set; }

        public CameraConnectionEditorViewModel(CameraViewModel cameraVM, ConnectionConfiguration connectionConfiguration)
        {
            manager = resolver.GetRequiredService<IConnectionConfigurationManager>();

            CameraVM = cameraVM;
            ConnectionConfiguration = connectionConfiguration;
        }

        protected override void SetupSubscriptions(CompositeDisposable d)
        {
            base.SetupSubscriptions(d);

            this.WhenAnyValue(x => x.ConnectionConfiguration)
                .Where(x => x != null)
                .Subscribe(x =>
                {
                    IsDebug = ConnectionConfiguration.IsDebug;
                    Name = ConnectionConfiguration?.Name;
                    URL = ConnectionConfiguration?.CameraConnection?.URL;
                    if (ConnectionConfiguration?.CameraConnection != null)
                    {
                        CameraInputHeight = ConnectionConfiguration.CameraConnection.InputHeight;
                        CameraInputWidth = ConnectionConfiguration.CameraConnection.InputWidth;
                    }

                    this.WhenAnyValue(x => x.Name)
                        .Subscribe(name =>
                        {
                            if (ConnectionConfiguration != null)
                            {
                                ConnectionConfiguration.Name = name;
                            }
                        })
                        .DisposeWith(d);

                    this.WhenAnyValue(x => x.URL)
                        .Subscribe(url =>
                        {
                            if (ConnectionConfiguration?.CameraConnection != null)
                            {
                                ConnectionConfiguration.CameraConnection.URL = url;
                            }
                        })
                        .DisposeWith(d);

                    this.WhenAnyValue(x => x.IsDebug)
                        .Subscribe(isDebug =>
                        {
                            CameraVM.IsDebug = isDebug;
                            if (ConnectionConfiguration != null)
                            {
                                ConnectionConfiguration.IsDebug = isDebug;
                            }
                        })
                        .DisposeWith(d);

                    x.CameraConnection.WhenAnyValue(x => x.InputHeight)
                        .Where(x => x > 0)
                        .Subscribe(height =>
                        {
                            CameraInputHeight = height;
                        })
                        .DisposeWith(d);

                    x.CameraConnection.WhenAnyValue(x => x.InputWidth)
                        .Where(x => x > 0)
                        .Subscribe(width =>
                        {
                            CameraInputWidth = width;
                        })
                        .DisposeWith(d);

                    /*                    this.WhenAnyValue(x => x.CameraInputHeight)
                                            .Subscribe(inputHeight =>
                                            {
                                                if (ConnectionConfiguration?.CameraConnection != null)
                                                    ConnectionConfiguration.CameraConnection.InputHeight = inputHeight;
                                            })
                                            .DisposeWith(d);

                                        this.WhenAnyValue(x => x.CameraInputWidth)
                                            .Subscribe(inputWidth =>
                                            {
                                                if (ConnectionConfiguration?.CameraConnection != null)
                                                    ConnectionConfiguration.CameraConnection.InputWidth = inputWidth;
                                            })
                                            .DisposeWith(d);*/
                })
                .DisposeWith(d);

            this.WhenAnyValue(x => x.IsDebug)
                .Skip(1)
                .Subscribe(isDebug =>
                {
                    URL = string.Empty;
                })
                .DisposeWith(d);

            this.WhenAnyValue(x => x.ConnectionConfiguration)
                .Select(config => config.IsNewObject(manager))
                .ToPropertyEx(this, x => x.IsNewConfiguration)
                .DisposeWith(d);

            this.WhenAnyValue(x => x.CameraInputWidth, x => x.CameraInputHeight,
                (width, height) => width > 0 && height > 0 ? $"{width} x {height}" : "")
                .ToPropertyEx(this, x => x.CameraInputDimens)
                .DisposeWith(d);
        }

        protected override void SetupCommands()
        {
            base.SetupCommands();

            OpenCalibrationViewCommand = ReactiveCommand.Create(OpenCalibrationView);
            SelectImageFileCommand = ReactiveCommand.Create(SelectImageFile);
        }

        private void OpenCalibrationView()
        {
            var calibrationVM = new CalibrationViewModel(ConnectionConfiguration);
            IScreen screen = resolver.GetRequiredService<IScreen>();
            screen.Router.Navigate.Execute(calibrationVM);
        }

        private void SelectImageFile()
        {
            loadFileConfirm
                .Handle(Unit.Default)
                .Subscribe(str =>
                {
                    if (!string.IsNullOrEmpty(str))
                    {
                        URL = str;
                    }
                });
        }
    }
}
