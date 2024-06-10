using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using Emgu.CV;
using MaterialDesignThemes.Wpf;
using Opc.UaFx;
using ProbeSampler.Core.Services.Processing.Core;
using ProbeSampler.Presentation.Helpers;

namespace ProbeSampler.Presentation
{
    public class SamplerViewModel : ViewModelBase, ITabContent, IDisposable
    {
        private readonly IConnectionConfigurationManager manager;
        private readonly IPathService pathService;
        private readonly IAdminAccessService adminAccessService;
        private readonly IApplicationSettingsManager applicationSettingsManager;
        private BoxSearchProcessor? boxSearchProcessor;
        private YOLOv8Processor? yolov8Processor;
        private ImageRotationProcessor? imageRotationProcessor;
        private UndistortProcessor? undistortProcessor;
        private readonly SemaphoreSlim savingSemaphore = new(1, 1);
        private Mat lastProcessedFrame = new();

        private ConnectionConfiguration? connectionConfiguration;
        public ConnectionConfiguration? ConnectionConfiguration
        {
            get => connectionConfiguration;
            set
            {
                connectionConfiguration = value;

            }
        }
        [ObservableAsProperty] public bool CanCamOpera { get; }

        #region GridOverlay

        public List<Cell> Cells { get; private set; } = new();

        private int CellsNumberToSelect = 8;
        private int CellsToSelectMultiplier = 1;

        [Reactive] public bool GridOverlayVisibility { get; set; } = true;
        [Reactive] public bool ManualCellsSelection { get; set; } = false;
        [Reactive] public bool isPasswordValidationSuccessfully { get; set; } = false;
        [Reactive] public double GridOverlayControlX { get; set; }
        [Reactive] public double GridOverlayControlY { get; set; }
        [Reactive] public double CameraViewHeight { get; set; }
        [Reactive] public double CameraViewWidth { get; set; }
        [Reactive] public double GridOverlayWidth { get; set; }
        [Reactive] public double GridOverlayHeight { get; set; }
        [Reactive] public int GridOverlayCellHeight { get; set; }
        [Reactive] public string Password { get; set; }
        [Reactive] public PasswordMenuStatus PasswordStatus { get; set; } = PasswordMenuStatus.Closed;
        public ReactiveCommand<Unit, Unit>? SelectRandomCellsCommand { get; private set; }
        public ReactiveCommand<Unit, Unit>? ResetSelectedCellsCommand { get; private set; }
        public ReactiveCommand<Unit, Unit>? EnableManualSelectionCommand { get; private set; }
        public ReactiveCommand<Unit, Unit>? OffManualSelectionCommand { get; private set; }
        public ReactiveCommand<Unit, Unit>? ChangeSelectionMethodCommand { get; private set; }


        #endregion

        #region Camera

        public CameraViewModel CameraVM { get; private set; }
        [Reactive] public string? CameraURL { get; set; }
        private BehaviorSubject<bool> IsFrameSaving { get; } = new BehaviorSubject<bool>(false);
        public ReactiveCommand<Unit, Unit>? StartCommand { get; set; }
        public ReactiveCommand<Unit, Unit>? StopCommand { get; set; }
        public ReactiveCommand<Unit, Unit>? ConnectCommand { get; set; }
        public ReactiveCommand<Unit, Unit>? CancelConnectCommand { get; set; }
        public ReactiveCommand<Unit, Unit>? PauseCommand { get; set; }
        public ReactiveCommand<Unit, Unit>? SaveFrameCommand { get; private set; }
        public ReactiveCommand<Unit, Unit>? SavePreprocessedFrameCommand { get; private set; }

        #endregion

        #region OPC UA

        private readonly IOpcUaClient opcUaClient;
        public ReactiveCommand<Unit, Unit>? OpcConnectCommand { get; private set; }
        public ReactiveCommand<Unit, Unit>? OpcDisconnectCommand { get; private set; }
        public ReactiveCommand<Unit, Unit>? OpcTakeProbeCommand { get; private set; }
        [Reactive] public OPCUAConnectionStatus OPCUAConnectionStatus { get; set; }
        [Reactive] public string? OPCUAURL { get; set; }

        #endregion

        [Reactive] public DetectionType DetectionType { get; set; }
        [Reactive] public bool DetectionSwitch { get; set; }
        [Reactive] public ISBMessageSender? MessageSender { get; set; }

        public SamplerViewModel(Guid id, string identifier)
        {
            CameraVM = resolver.GetRequiredService<CameraViewModel>();

            CameraVM.ImageProcessingPipeline += CameraVM_ImageProcessingPipeline;

            pathService = resolver.GetRequiredService<IPathService>();
            manager = resolver.GetRequiredService<IConnectionConfigurationManager>();
            opcUaClient = resolver.GetRequiredService<IOpcUaClient>();
            applicationSettingsManager = resolver.GetRequiredService<IApplicationSettingsManager>();
            adminAccessService = resolver.GetRequiredService<IAdminAccessService>();

            ConnectionConfiguration = manager.Get(id);
            if (ConnectionConfiguration != null)
            {
                CameraURL = ConnectionConfiguration.CameraConnection.URL;
                GridOverlayControlX = ConnectionConfiguration.CameraConnection.GridOverlayX;
                GridOverlayControlY = ConnectionConfiguration.CameraConnection.GridOverlayY;
                CameraViewHeight = ConnectionConfiguration.CameraConnection.ViewHeight;
                CameraViewWidth = ConnectionConfiguration.CameraConnection.InputWidth;
                GridOverlayWidth = ConnectionConfiguration.CameraConnection.GridOverlayWidth;
                GridOverlayHeight = ConnectionConfiguration.CameraConnection.GridOverlayHeight;
                GridOverlayCellHeight = ConnectionConfiguration.CameraConnection.GridOverlayCellHeight;
                CameraVM.IsDebug = ConnectionConfiguration.IsDebug;
            }
            CellsHelper.UpdateCells(Cells, GridOverlayControlX, GridOverlayControlY, GridOverlayWidth, GridOverlayHeight, GridOverlayCellHeight);
        }

        private Mat CameraVM_ImageProcessingPipeline(Mat input)
        {
            var output = new Mat();

            if (imageRotationProcessor != null)
            {
                output = imageRotationProcessor.ReceiveImage(input);
            }
            if (undistortProcessor != null)
            {
                output = undistortProcessor.ReceiveImage(output.IsEmpty ? input : output);
            }

            lock (lastProcessedFrame)
            {
                lastProcessedFrame.Dispose();
                lastProcessedFrame = output.Clone();
            }

            if (boxSearchProcessor != null && DetectionType == DetectionType.OpenCVBoxSearch)
            {
                output = boxSearchProcessor.ReceiveImage(output.IsEmpty ? input : output);
            }
            if (yolov8Processor != null && DetectionType == DetectionType.Yolov8)
            {
                output = yolov8Processor.ReceiveImage(output);
                var boxes = (yolov8Processor as IProvideBoxes)?.GetDetections();
                //foreach (var box in boxes)
                //{
                //    var test = box.Width;
                //}
                if (boxes != null)
                {
                    CellsToSelectMultiplier = boxes.Count() > 1 || boxes.Any(box => box.Width > 600) ? 2 : 1;
                    CellsHelper.UpdateCellsState(Cells, boxes);
                }
            }
            return output;
        }

        public void Dispose()
        {
            opcUaClient.Dispose();
            CameraVM.ImageProcessingPipeline -= CameraVM_ImageProcessingPipeline;
            CameraVM.Dispose();
            lastProcessedFrame.Dispose();
            savingSemaphore.Dispose();
        }

        protected override void SetupSubscriptions(CompositeDisposable d)
        {
            base.SetupSubscriptions(d);

            imageRotationProcessor = new ImageRotationProcessor()
                .DisposeWith(d);
            undistortProcessor = new UndistortProcessor()
                .DisposeWith(d);
            boxSearchProcessor = new BoxSearchProcessor()
                .DisposeWith(d);
            yolov8Processor = new YOLOv8Processor(pathService.YoloDetectPath,
                ConnectionConfiguration?.CameraConnection.Scale ?? 0, ConnectionConfiguration?.ConfidenceFromConfig ?? 0)
                .DisposeWith(d);

            this.WhenAnyValue(x => x.ConnectionConfiguration)
                .Where(x => x != null)
                .Subscribe(connection =>
                {
                    if (connection?.CameraConnection != null)
                    {
                        if (undistortProcessor != null)
                        {
                            undistortProcessor.CameraMatrixArray = connection.CameraConnection.CameraMatrix;
                            undistortProcessor.DistCoeffsArray = connection.CameraConnection.DistCoeff;
                            undistortProcessor.Alpha = connection.CameraConnection.Alpha;
                        }
                        if (imageRotationProcessor != null)
                        {
                            imageRotationProcessor.Angle = connection.CameraConnection.RotationAngle;
                        }
                        if (boxSearchProcessor != null)
                        {
                            boxSearchProcessor.ActiveROIX = connection.CameraConnection.GridOverlayX;
                            boxSearchProcessor.ActiveROIY = connection.CameraConnection.GridOverlayY;
                            boxSearchProcessor.ActiveROIHeight = connection.CameraConnection.GridOverlayHeight;
                        }
                    }

                    if (connection?.SamplerConnection != null)
                        Task.Run(() => opcUaClient.Initialize(connection.SamplerConnection))
                        .DisposeWith(d);
                    if (connection?.SamplerConnection?.URL != null)
                        OPCUAURL = connection.SamplerConnection.URL;

                    connection.WhenAnyValue(x => x.Name)
                        .Select(name => name)
                        .ToPropertyEx(this, x => x.ViewName)
                        .DisposeWith(d);
                })
                .DisposeWith(d);

            this.WhenAnyValue(x => x.DetectionSwitch)
                .Subscribe(switchValue =>
                {
                    //DetectionType = switchValue ? DetectionType.OpenCVBoxSearch : DetectionType.Yolov8;
                    DetectionType = switchValue ? DetectionType.None : DetectionType.Yolov8;
                })
                .DisposeWith(d);
            /*            this.WhenAnyValue(x => x.DetectionType)
                            .Scan((previous: default(DetectionType), current: default(DetectionType)),
                                 (acc, cur) => (acc.current, cur))
                            .Skip(1)
                            .Subscribe(pair =>
                            {

                            })
                            .DisposeWith(d);*/
            opcUaClient.ConnectionStatus
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    OPCUAConnectionStatus = x;
                })
                .DisposeWith(d);

            CameraVM.WhenAnyValue(x => x.CameraState)
                .Select(state => state == CameraState.Running)
                .ToPropertyEx(this, x => x.CanCamOpera)
                .DisposeWith(d);

            this.WhenAnyValue(x => x.MessageSender)
                .Subscribe(msgSender =>
                {
                    CameraVM.MessageSender = MessageSender;
                })
                .DisposeWith(d);
        }

        protected override void SetupCommands()
        {
            base.SetupCommands();

            var cancelSubject = new Subject<Unit>();

            var canConnect = CameraVM.WhenAnyValue(x => x.CameraState)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x == CameraState.Stopped || x == CameraState.Error);

            ConnectCommand = ReactiveCommand.CreateFromObservable(() =>
                Observable.StartAsync(ct => CameraVM.Connect(CameraURL ?? "", ct))
                .TakeUntil(cancelSubject), canConnect);

            var canStop = CameraVM.WhenAnyValue(x => x.CameraState)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x == CameraState.Running);

            StopCommand = ReactiveCommand.Create(CameraVM.Stop, canStop);

            var canStart = CameraVM.WhenAnyValue(x => x.CameraState)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x == CameraState.Pause);

            StartCommand = ReactiveCommand.Create(CameraVM.Start, canStart);

            var canPause = CameraVM.WhenAnyValue(x => x.CameraState)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x == CameraState.Running);

            PauseCommand = ReactiveCommand.Create(CameraVM.Pause, canPause);

            CancelConnectCommand = ReactiveCommand.Create(
                () => cancelSubject.OnNext(Unit.Default),
                ConnectCommand.IsExecuting);

            //CameraVM.CancelConnectCommand = CancelConnectCommand;

            SaveFrameCommand = ReactiveCommand.CreateFromTask(async () => { await CameraVM.SaveFrame(); });

            var canSavePreprocessedFrame = this.WhenAnyValue(x => x.CanCamOpera, x => x.IsFrameSaving)
                .Select(x => x.Item1 && x.Item2.Value == false);

            SavePreprocessedFrameCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                if (await savingSemaphore.WaitAsync(0))
                {
                    try
                    {
                        IsFrameSaving.OnNext(true);

                        Mat frameToSave;
                        lock (lastProcessedFrame)
                        {
                            frameToSave = lastProcessedFrame.Clone();
                        }
                        await Task.Run(() => SavePreprocessedFrame(frameToSave));
                    }
                    finally
                    {
                        IsFrameSaving.OnNext(false);
                        savingSemaphore.Release();
                    }
                }
            }, canSavePreprocessedFrame);

            var canInteractSells = this.WhenAnyValue(x => x.GridOverlayVisibility)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x);

            SelectRandomCellsCommand = ReactiveCommand.CreateFromTask(async () => await Task.Run(() => CellsHelper.SelectRandomCells(Cells, CellsNumberToSelect * CellsToSelectMultiplier)), canInteractSells);

            ResetSelectedCellsCommand = ReactiveCommand.CreateFromTask(async () => await Task.Run(() => CellsHelper.ResetSelectedCells(Cells)), canInteractSells);
            EnableManualSelectionCommand = ReactiveCommand.Create(() => EnableManualSelection(Password), canInteractSells);
            OffManualSelectionCommand = ReactiveCommand.Create(() => OffManualSelection(), canInteractSells);

            //var cancelOpcSubject = new Subject<Unit>();

            var canOpcConnect = this.WhenAnyValue(x => x.OPCUAConnectionStatus)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x == OPCUAConnectionStatus.Disconnected
                || x == OPCUAConnectionStatus.Error
                || x == OPCUAConnectionStatus.Created
                || x == OPCUAConnectionStatus.None);

            OpcConnectCommand = ReactiveCommand.CreateFromTask(async () => await Task.Run(() => OpcConnect()), canOpcConnect);

            var canOpcDisconnect = this.WhenAnyValue(x => x.OPCUAConnectionStatus)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x == OPCUAConnectionStatus.Connected);

            OpcTakeProbeCommand = ReactiveCommand.CreateFromTask(async () => await Task.Run(() => OpcTakeProbe(CellsHelper.GetSelectedCells(Cells))), canOpcDisconnect);

            OpcDisconnectCommand = ReactiveCommand.CreateFromTask(async () => await Task.Run(() => OpcDisconnect()), canOpcDisconnect);

            ChangeSelectionMethodCommand = ReactiveCommand.Create(ChangeSelectionMethod);
        }

        protected override void SetupStart()
        {
            base.SetupStart();

            //OpcConnectCommand?.Execute();
        }

        //TODO добавить проверку на Url как здесь CameraVM.Connect(CameraURL ?? "", ct)
        private async Task OpcConnect()
        {
            try
            {
                opcUaClient?.Connect();
                this.Log().Info("OPC Client connected");
            }
            catch (OpcException ex)
            {
                this.Log().Error($"Error while connecting OPC Server: {ex}");
                this.SendMessageToBus("Не удалось подключиться к OPC серверу");
            }
        }
        private async Task OpcDisconnect()
        {
            try
            {
                opcUaClient?.Disconnect();
            }
            catch (OpcException ex)
            {
                this.Log().Error($"Error while disconnection from OPC Server: {ex}");
                this.SendMessageToBus("Не удалось отключиться от OPC сервера");
            }
        }
        //TODO добавить статус проотборинка на основе status_sps
        private async Task OpcTakeProbe(List<Cell> SelectedCells)
        {
            try
            {
                if (SelectedCells is null || SelectedCells.Count == 0)
                {
                    this.SendMessageToBus("Не выбрано ни одной клетки");
                    return;
                }
                //todo add out of servece exception handler

                //if (opcUaClient.ReadValue<int>("ns=1;s=SIEMENSPLC.siemensplc.status_sps") == 2)
                //{
                //    this.Log().Warn("Пробоотборник работает невозможно подготовить значения для Opc сервера");
                //    this.SendMessageToBus("пробоотборник работает");
                //    return;
                //}
                //if (opcUaClient.ReadValue<int>("ns=1;s=SIEMENSPLC.siemensplc.status_sps") == 1)
                //{
                //    //TODO add message
                //    this.SendMessageToBus("данные уже записаны, нужно просто нажать кнопку старта, переписать значения точек?");
                //    return;
                //}
                // if 255:

                byte probesNumber = Convert.ToByte(SelectedCells.Count);
                byte probesCounter = 1;
                bool isNeedRotationOver90Degrees = false;
                byte aktionen = 1;

                foreach (Cell cell in SelectedCells)
                {
                    isNeedRotationOver90Degrees = cell.X > ConnectionConfiguration?.SamplerConnection.UnreachablePixels;

                    double rotation = ConvectorHelper.ConvertPixelsYToRotation(
                            yPixels: cell.Y,
                            a: ConnectionConfiguration?.SamplerConnection.RotationCalculationCoeffA,
                            b: ConnectionConfiguration?.SamplerConnection.RotationCalculationCoeffB,
                            c: ConnectionConfiguration?.SamplerConnection.RotationCalculationCoeffC,
                            isOver90Degrees: isNeedRotationOver90Degrees);

                    double offset = ConvectorHelper.ConvertPixelsXToLinearDisplacement(
                            xPixels: cell.X,
                            a: ConnectionConfiguration?.SamplerConnection.LinearCalculationCoeffA,
                            b: ConnectionConfiguration?.SamplerConnection.LinearCalculationCoeffB,
                            CorrectionValue: ConvectorHelper.CalculateCorrectionValue(
                                rotation: rotation,
                                beakCoeff: ConnectionConfiguration?.SamplerConnection.BeakCoeff),
                            isOver90Degrees: isNeedRotationOver90Degrees);

                    opcUaClient.WriteValue($"ns=1;s=SIEMENSPLC.siemensplc.pos mast {probesCounter}", offset);

                    opcUaClient.WriteValue($"ns=1;s=SIEMENSPLC.siemensplc.pos kopf {probesCounter}", rotation);
                    probesCounter++;
                }
                opcUaClient.WriteValue("ns=1;s=SIEMENSPLC.siemensplc.anzahl_positiionen", probesNumber);
                opcUaClient.WriteValue("ns=1;s=SIEMENSPLC.siemensplc.aktionen", aktionen);
            }
            catch (Exception ex)
            {
                this.Log().Error($"Ошибка при задании значений контроллера пробоотборника: {ex}");
                this.SendMessageToBus("Не удалось подготовить пробоотборник к взятию пробы");
            }
        }

        private void SavePreprocessedFrame(Mat frame)
        {
            using (frame)
            {
                try
                {
                    string path = SaveFrameCore(frame);

                    this.Log().Info($"Frame saved to {path}");
                    var message = new SBMessage(
                        messageText: "Кадр успешно сохранен",
                        actionCaption: "ОТКРЫТЬ В ПРОВОДНИКЕ",
                        action: (object? argument) =>
                            {
                                Process.Start("explorer.exe", $"/select,\"{path}\"");
                            },
                        durationOverride: TimeSpan.FromSeconds(10)
                        );
                    this.SendMessageToBus(message);
                }
                catch (Exception ex)
                {
                    this.Log().Error($"Error on saving frame: {ex}");
                    this.SendMessageToBus("Не удалось захватить изображение");
                }
            }

            IsFrameSaving.OnNext(false);
        }

        /// <summary>
        /// Общая функция сохранения кадра
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        private string SaveFrameCore(Mat frame)
        {
            var captureDate = DateTime.Now;

            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

            var dirName = !string.IsNullOrEmpty(ConnectionConfiguration?.Name) ? $"{ConnectionConfiguration.Name}_{captureDate:ddMMyy}" : $"{captureDate:ddMMyy}";
            dirName = Regex.Replace(dirName, invalidRegStr, "");

            var pathToDir = Path.Combine(pathService.CapturesPath, dirName);
            if (!Directory.Exists(pathToDir))
            {
                Directory.CreateDirectory(pathToDir);
            }
            var path = Path.Combine(pathToDir, $"screenshot_{captureDate:MMddyy_HHmmss}.jpg");
            CvInvoke.Imwrite(path, frame);
            return path;
        }

        private void EnableManualSelection(string password)
        {
            try
            {
                var settings = applicationSettingsManager.GetSettings();

                if (Password != null && settings != null)

                {
                    if (!PasswordHasher.VerifyIdentityV3Hash(Password, settings.AdminPassword))
                    {
                        isPasswordValidationSuccessfully = false;
                        return;
                    };
                    isPasswordValidationSuccessfully = true;
                }
            }
            catch
            {
                isPasswordValidationSuccessfully = false;
            }
        }
        private void OffManualSelection()
        {
            isPasswordValidationSuccessfully = false;
        }

        private async void ChangeSelectionMethod()
        {
            bool isGranted = adminAccessService.IsAccessGranted;
            bool isPassEntered = false;
            if (!isGranted)
            {
                isPassEntered = await ShowPasswordCheck();
            }
            if (!isPassEntered && !isGranted) return;
        }

        private async Task<bool> ShowPasswordCheck()
        {
            var view = resolver.GetRequiredService<IViewFor<AdminPasswordRequestViewModel>>();
            AdminPasswordRequestViewModel viewModel = resolver.GetRequiredService<AdminPasswordRequestViewModel>();
            view.ViewModel = viewModel;
            var result = await DialogHost.Show(view, "ChildDialog");
            return result is bool bResult && bResult;
        }
    }
}
