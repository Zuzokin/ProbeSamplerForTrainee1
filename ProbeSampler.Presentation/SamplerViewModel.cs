using Emgu.CV;
using Opc.Ua;
using ProbeSampler.Core.Services.Processing.Core;
using ProbeSampler.Presentation.Helpers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ProbeSampler.Presentation
{
    public class SamplerViewModel : ViewModelBase, ITabContent, IDisposable
    {
        private readonly IConnectionConfigurationManager manager;
        private readonly IPathService pathService;
        // private readonly IAdminAccessService adminAccessService;
        // private readonly IApplicationSettingsManager applicationSettingsManager;
        // private BoxSearchProcessor? boxSearchProcessor;
        private YOLOv8Processor? yolov8Processor;
        private ImageRotationProcessor? imageRotationProcessor;
        private UndistortProcessor? undistortProcessor;
        private readonly SemaphoreSlim savingSemaphore = new(1, 1);
        private Mat lastProcessedFrame = new();
        private readonly object lockObj = new object();

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
        public List<CalibrationCircle> Circles { get; private set; } = new List<CalibrationCircle>
        {
            new CalibrationCircle(1561, 690, 50, 50),
            new CalibrationCircle(1561, 510, 50, 50),
            new CalibrationCircle(931, 600, 50, 50),
            new CalibrationCircle(241, 510, 50, 50),
            new CalibrationCircle(241, 690, 50, 50)
        };

        public int? CellsNumberToSelect = 7;
        private double? CellsToSelectBaseMultiplier = 1;
        private double? CellsToSelectMultiplier = 1;
        private int? BigTruckWidth = 600;
        //todo implement TSP problem algo
        private double? OffsetVelocity;
        private double? RotationVelocity;
        private IEnumerable<BoundingBox>? currentBodyBoxes = null;
        private IEnumerable<BoundingBox>? currentCrossbarsBoxes = null;

        [Reactive] public CrossbarType CrossbarType { get; set; }

        // [Reactive] public bool  { get; set; } = false;

        [Reactive] public bool GridOverlayVisibility { get; set; } = true;
        [Reactive] public bool CalibrationCirclesVisibility { get; set; } = false;

        [Reactive] public bool CrossbarSwitch { get; set; } = false;
        [Reactive] public bool IsDetectionsFixed { get; set; } = false;
        [Reactive] public bool IsPasswordValidationSuccessfully { get; set; } = false;
        [Reactive] public BehaviorSubject<bool> isManualSelectionOn { get; set; } = new BehaviorSubject<bool>(false);

        [Reactive] public double GridOverlayControlX { get; set; }

        [Reactive] public double GridOverlayControlY { get; set; }

        [Reactive] public double CameraViewHeight { get; set; }

        [Reactive] public double CameraViewWidth { get; set; }

        [Reactive] public double GridOverlayWidth { get; set; }

        [Reactive] public double GridOverlayHeight { get; set; }

        [Reactive] public int GridOverlayCellHeight { get; set; }

        public ReactiveCommand<Unit, Unit>? SelectRandomCellsCommand { get; private set; }

        public ReactiveCommand<Unit, Unit>? ResetSelectedCellsCommand { get; private set; }

        // public ReactiveCommand<Unit, Unit>? EnableManualSelectionCommand { get; private set; }
        public ReactiveCommand<Unit, Unit>? OffManualSelectionCommand { get; private set; }

        public ReactiveCommand<Unit, Unit>? ChangeSelectionMethodCommand { get; private set; }

        public ReactiveCommand<Unit, Unit>? CheckCalibrationCommand { get; private set; }

        public ReactiveCommand<Unit, Unit>? FixDetectionsCommand { get; private set; }

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
        [Reactive] public SpsStatus OpcSpsStatus { get; set; } = SpsStatus.Waiting;
        [Reactive] public bool OpcFailureStatus { get; set; }
        [Reactive] public bool IsSampleAvailable { get; set; } = true;
        //[Reactive] public bool IncStatus { get; set; }
        [Reactive] public bool IsPositionValuesSet { get; set; } = false;
        [Reactive] public bool GateFailureValue { get; set; }

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
            // applicationSettingsManager = resolver.GetRequiredService<IApplicationSettingsManager>();
            // adminAccessService = resolver.GetRequiredService<IAdminAccessService>();

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

                CellsNumberToSelect = ConnectionConfiguration.SamplerConnection.CellsNumberToSelect;
                CellsToSelectBaseMultiplier = ConnectionConfiguration.SamplerConnection.CellsToSelectMultiplier;
                BigTruckWidth = ConnectionConfiguration.SamplerConnection.BigTruckWidth;
                OffsetVelocity = ConnectionConfiguration.SamplerConnection.OffsetVelocity;
                RotationVelocity = ConnectionConfiguration.SamplerConnection.RotationVelocity;
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

            lock (lockObj)
            {
                lastProcessedFrame.Dispose();
                lastProcessedFrame = output.Clone();
            }

            // if (boxSearchProcessor != null && DetectionType == DetectionType.OpenCVBoxSearch)
            // {
            //    output = boxSearchProcessor.ReceiveImage(output.IsEmpty ? input : output);
            // }
            if (yolov8Processor != null && DetectionType == DetectionType.Yolov8)
            {
                output = yolov8Processor.ReceiveImage(output);
                currentBodyBoxes = (yolov8Processor as IProvideBoxes)?.GetDetections("body");

                CellsNumberToSelect = CellsHelper.GetCellNumberToSelect(currentBodyBoxes);
                //CellsToSelectMultiplier = boxes.Count() > 1 || boxes.Any(box => box.Width > BigTruckWidth) ? CellsToSelectBaseMultiplier : 1;
                CellsHelper.UpdateCellsState(Cells, currentBodyBoxes, IsDetectionsFixed);
//#if (DEBUG)
                currentCrossbarsBoxes = (yolov8Processor as IProvideBoxes)?.GetDetections("crossbar");
                CellsHelper.UpdateCrossbarsState(Cells, currentCrossbarsBoxes);
//#endif
            }

            return output;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            opcUaClient.DisposeAsync();
            CameraVM.ImageProcessingPipeline -= CameraVM_ImageProcessingPipeline;
            CameraVM.Dispose();
            lastProcessedFrame.Dispose();
            savingSemaphore.Dispose();
            //IsFrameSaving.OnCompleted();
            IsFrameSaving.Dispose();
            //isManualSelectionOn.OnCompleted();
            isManualSelectionOn.Dispose();
            // yolov8Processor?.Dispose();
            // undistortProcessor?.Dispose();
        }

        protected override void SetupSubscriptions(CompositeDisposable d)
        {
            base.SetupSubscriptions(d);

            imageRotationProcessor = new ImageRotationProcessor()
                .DisposeWith(d);
            undistortProcessor = new UndistortProcessor()
                .DisposeWith(d);
            // boxSearchProcessor = new BoxSearchProcessor()
            //    .DisposeWith(d);
            yolov8Processor = new YOLOv8Processor(
                pathService.YoloDetectPath,
                ConnectionConfiguration?.CameraConnection.Scale ?? 0, ConnectionConfiguration?.ConfidenceFromConfig ?? 0.6f)
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

                        // if (boxSearchProcessor != null)
                        // {
                        //    boxSearchProcessor.ActiveROIX = connection.CameraConnection.GridOverlayX;
                        //    boxSearchProcessor.ActiveROIY = connection.CameraConnection.GridOverlayY;
                        //    boxSearchProcessor.ActiveROIHeight = connection.CameraConnection.GridOverlayHeight;
                        // }
                    }

                    if (connection?.SamplerConnection != null)
                    {
                        Task.Run(() => opcUaClient.Initialize(connection.SamplerConnection))
                        .DisposeWith(d);
                    }

                    if (connection?.SamplerConnection?.URL != null)
                    {
                        OPCUAURL = connection.SamplerConnection.URL;
                    }

                    connection.WhenAnyValue(x => x.Name)
                        .Select(name => name)
                        .ToPropertyEx(this, x => x.ViewName)
                        .DisposeWith(d);
                })
                .DisposeWith(d);

            this.WhenAnyValue(x => x.DetectionSwitch)
                .Subscribe(switchValue =>
                {
                    // DetectionType = switchValue ? DetectionType.OpenCVBoxSearch : DetectionType.Yolov8;
                    DetectionType = switchValue ? DetectionType.None : DetectionType.Yolov8;
                })
                .DisposeWith(d);
            this.WhenAnyValue(x => x.CrossbarSwitch)
                .Subscribe(switchValue =>
                {
                    CrossbarType = switchValue ? CrossbarType.Horizontal : CrossbarType.Vertical;
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

            opcUaClient.SpsStatusValue
                .ObserveOn(RxApp.MainThreadScheduler)                
                .Subscribe(async x =>
                {
                    if (OpcSpsStatus == x || x == 0)
                    {
                        return;
                    }

                    OpcSpsStatus = x;

                    if (opcUaClient != null && OPCUAConnectionStatus == OPCUAConnectionStatus.Connected)
                    {
                        try
                        {
                            if (OpcSpsStatus == SpsStatus.Completed)
                            {
                                if(CalibrationCirclesVisibility)
                                    CalibrationCirclesVisibility = false;

                                // todo передать значения, что отбор закончился
                                // сделать и вызывать метод opcUaClient?
                                // todo where is no need in is sample taken but i dont want to remove it, smth might broken
                                await opcUaClient.WriteValue("ns=1;s=SIEMENSPLC.siemensplc.is sample taken", true);
                                await opcUaClient.ResetValues(14);
                                CellsHelper.ResetSelectedCells(Cells);
                                this.Log().Info($"reset opc server values and selected cells");
                                this.Log().Info("Samples taken successfully");
                                //opcUaClient.WriteValue("ns=1;s=SIEMENSPLC.siemensplc.status_sps", 255);
                            }
                            else
                            {
                                await opcUaClient.WriteValue("ns=1;s=SIEMENSPLC.siemensplc.status_sps", (byte)OpcSpsStatus);
                            }
                        }
                        catch (Exception ex)
                        {
                            this.Log().Error($"Error on reset values on OpcServer", ex);
                            this.SendMessageToBus($"{ex.Message}");
                        }
                    }
                })
                .DisposeWith(d);
            //todo this opcuaclient connected doest work, its only shows true, but if delete opc server its never changes to false
            this.WhenAnyValue(x => x.opcUaClient.Client)                
                .WhereNotNull()
                .Subscribe(client =>
                {
                    if (client.Connected)
                    {
                        opcUaClient.ConnectionStatus.OnNext(OPCUAConnectionStatus.Connected);
                        this.Log().Info($"opc client connected");
                        //this.SendMessageToBus($"connected");

                    }
                    else
                    {
                        opcUaClient.ConnectionStatus.OnNext(OPCUAConnectionStatus.Disconnected);
                        this.Log().Info($"opc client disconnected");
                    }
                })
                .DisposeWith(d);

            opcUaClient.IsSampleAvailable
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async x =>
                {
                    if (IsSampleAvailable == x)
                    {
                        return;
                    }
                    if (IsSampleAvailable == false && x == true && IsPositionValuesSet == true)
                    {
                        await opcUaClient.WriteValue("ns=1;s=SIEMENSPLC.siemensplc.aktionen", 1);
                        opcUaClient.SpsStatusValue.OnNext(SpsStatus.InProgress);
                        this.Log().Info($"wait for operator confirmation to run probe sampler");
                    }
                    IsSampleAvailable = x;
                })
                .DisposeWith(d);

            opcUaClient.FailureValue
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    if (OpcFailureStatus == x)
                    {
                        return;
                    }

                    OpcFailureStatus = x;

                    // true - ошибка, false - всё ок
                    if (OpcFailureStatus)
                    {
                        opcUaClient?.ConnectionStatus.OnNext(OPCUAConnectionStatus.ErrorNoConnectionToProbe);
                        this.Log().Error($"Opc server lost connection to probe sampler");
                        this.SendMessageToBus("Opc сервер потерял соединение с пробоотборником");
                    }
                    else
                    {
                        if (opcUaClient.Client.Connected)
                        {
                            opcUaClient?.ConnectionStatus.OnNext(OPCUAConnectionStatus.Connected);
                            this.Log().Info($"Opc client connected");
                        }
                    }
                })
                .DisposeWith(d);

            opcUaClient.GateFailureValue
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    if (GateFailureValue == x)
                    {
                        return;
                    }

                    GateFailureValue = x;

                    // true - ошибка, false - всё ок
                    if (GateFailureValue)
                    {
                        opcUaClient?.ConnectionStatus.OnNext(OPCUAConnectionStatus.ErrorNoConnectionToGate);
                        this.Log().Error($"Opc server lost connection to probe sampler");
                        this.SendMessageToBus("Opc сервер потерял соединение с пробоотборником");
                    }
                    else
                    {
                        if (opcUaClient.Client.Connected)
                        {
                            opcUaClient?.ConnectionStatus.OnNext(OPCUAConnectionStatus.Connected);
                            this.Log().Error($"Opc server reconnected to probe sampler");
                        }
                    }
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

            isManualSelectionOn
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(x =>
                {
                    IsPasswordValidationSuccessfully = x;
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

            ConnectCommand = ReactiveCommand.CreateFromObservable(
                () =>
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

            // CameraVM.CancelConnectCommand = CancelConnectCommand;

            SaveFrameCommand = ReactiveCommand.CreateFromTask(async () => { await CameraVM.SaveFrame(); });
            // todo разобраться, почему кнопка сохранения неактивна
            //var canSavePreprocessedFrame = this.WhenAnyValue(x => x.CanCamOpera, x => x.IsFrameSaving)
            //    .Select(x => x.Item1 && x.Item2.Value);

            SavePreprocessedFrameCommand = ReactiveCommand.CreateFromTask(
                async () =>
            {
                if (await savingSemaphore.WaitAsync(0))
                {
                    try
                    {
                        IsFrameSaving.OnNext(true);

                        Mat frameToSave;
                        lock (lockObj)
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
            }, canPause);

            var canInteractSells = this.WhenAnyValue(x => x.GridOverlayVisibility)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x);

            SelectRandomCellsCommand = ReactiveCommand.CreateFromTask(async () => await Task.Run(() => CellsHelper.SelectRandomCells(Cells, currentBodyBoxes)), canInteractSells);

            ResetSelectedCellsCommand = ReactiveCommand.CreateFromTask(async () => await Task.Run(() => CellsHelper.ResetSelectedCells(Cells)), canInteractSells);
            // EnableManualSelectionCommand = ReactiveCommand.Create(() => EnableManualSelection(Password), canInteractSells);
            OffManualSelectionCommand = ReactiveCommand.Create(() => OffManualSelection(), canInteractSells);

            // var cancelOpcSubject = new Subject<Unit>();

            var canOpcConnect = this.WhenAnyValue(x => x.OPCUAConnectionStatus)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x == OPCUAConnectionStatus.Disconnected
                || x == OPCUAConnectionStatus.Error
                || x == OPCUAConnectionStatus.Created
                || x == OPCUAConnectionStatus.None);

            OpcConnectCommand = ReactiveCommand.CreateFromTask(async () => await Task.Run(() => OpcConnect()), canOpcConnect);

            var canOpcDisconnect = this.WhenAnyValue(x => x.OPCUAConnectionStatus)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x == OPCUAConnectionStatus.Connected || x == OPCUAConnectionStatus.ErrorNoConnectionToProbe);

            OpcTakeProbeCommand = ReactiveCommand.CreateFromTask(async () => await Task.Run(() => OpcTakeProbe(CellsHelper.GetSelectedCells(Cells))), canOpcDisconnect);

            OpcDisconnectCommand = ReactiveCommand.CreateFromTask(async () => await Task.Run(() => OpcDisconnect()), canOpcDisconnect);

            ChangeSelectionMethodCommand = ReactiveCommand.Create(ChangeSelectionMethod);

            CheckCalibrationCommand = ReactiveCommand.Create(CheckCalibration);

            FixDetectionsCommand = ReactiveCommand.Create(FixDetections);
        }

        protected override void SetupStart()
        {
            base.SetupStart();

            // OpcConnectCommand?.Execute();
        }

        // TODO добавить проверку на Url как здесь CameraVM.Connect(CameraURL ?? "", ct)
        /// <summary>
        /// Подключится к OPC серверу
        /// </summary>
        private async Task OpcConnect()
        {
            try
            {
                await opcUaClient.Connect();
                this.Log().Info("OPC Client connected");
            }
            catch (UriFormatException ex)
            {
                this.Log().Error($"Error while connecting OPC Server: {ex}");
                this.SendMessageToBus("В настройках не указан uri OPC сервера или указан неверно");
            }
            catch (ServiceResultException ex)
            {
                this.Log().Error($"Error establishing a connection to OPC server: {ex}");
                this.SendMessageToBus("Ошибка установления соединения к OPC серверу: BadNotConnected");
            }
            catch (Exception ex)
            {
                // Opc.Ua.ServiceResultException
                this.Log().Error($"Error while connecting OPC Server: {ex}");
                this.SendMessageToBus("Не удалось подключиться к OPC серверу");
            }
        }

        /// <summary>
        /// отключиться от OPC сервера
        /// </summary>
        private async Task OpcDisconnect()
        {
            try
            {
                await opcUaClient.Disconnect();
                this.Log().Info($"opc client disconnected");
            }
            catch (Exception ex)
            {
                this.Log().Error($"Error while disconnection from OPC Server: {ex}");
                this.SendMessageToBus("Не удалось отключиться от OPC сервера");
            }
        }

        #region метод для взятия проб        

        // TODO добавить статус проотборинка на основе status_sps, невозможно тк status_sps не изменяется автоматически, когда заканчивает взятие проб
        /// <summary>
        /// Метод для отбора проб
        /// </summary>
        /// <param name="SelectedCells">Список выбранных клеток</param>
        private async Task OpcTakeProbe(List<Cell> SelectedCells)
        {
            this.Log().Info($"Starts setting values to Opc, current boxes: {currentBodyBoxes}");
            try
            {
                if (!ValidateSelectedCells(SelectedCells)) return;

                if (IsSamplerInProgress()) return;

                if ((byte)OpcSpsStatus == 255)
                {
                    HandleManualSelection(SelectedCells);

                    byte probesNumber = Convert.ToByte(SelectedCells.Count);
                    this.Log().Info("Starts calculation of probe sampler values for selected cells");
                    CalculateProbeSamplerValues(SelectedCells);

                    this.Log().Info("Find shortest path");
                    var shortestPathCells = FindShortestPath(SelectedCells);

                    LogSelectedCells(shortestPathCells);

                    await WriteValuesToOpc(shortestPathCells, probesNumber);

                    this.Log().Info("Cells values set in Opc server");
                    await UpdateSamplerStatusAndLog(probesNumber);

                    if (isManualSelectionOn.Value)
                    {
                        DisableManualSelectionMode();
                    }
                    else
                    {
                        this.Log().Info("Values successfully transferred to the sampler");
                        this.SendMessageToBus("Значения успешно переданы в пробоотборник");
                    }

                    await LogOpcServerValues(probesNumber);
                    await SaveScreenshotForLogs();
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        /// <summary>
        /// Проверяет выбранные клетки.
        /// </summary>
        /// <param name="selectedCells">Список выбранных клеток</param>
        /// <returns>Истина, если клетки допустимы, иначе ложь</returns>
        private bool ValidateSelectedCells(List<Cell> selectedCells)
        {
            if (selectedCells is null || selectedCells.Count == 0)
            {
                this.SendMessageToBus("Не выбрано ни одной клетки");
                this.Log().Warn("No cells selected");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Проверяет, ведется ли в данный момент сбор проб.
        /// </summary>
        /// <returns>Истина, если пробоотборник работает, иначе ложь</returns>
        private bool IsSamplerInProgress()
        {
            if ((byte)OpcSpsStatus == 2)
            {
                this.Log().Warn("Sampler is in progress, unable to prepare values for Opc server");
                this.SendMessageToBus("Пробоотборник работает");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Обрабатывает ручной выбор клеток.
        /// </summary>
        /// <param name="selectedCells">Список выбранных клеток</param>
        private void HandleManualSelection(List<Cell> selectedCells)
        {
            if (!isManualSelectionOn.Value)
            {
                //needed only when has additional cells
                while (selectedCells.Count > CellsNumberToSelect.Value)
                {
                    var randomCellIndex = new Random().Next(0, selectedCells.Count);
                    CellsHelper.UpdateCell(selectedCells[randomCellIndex]);
                    selectedCells.RemoveAt(randomCellIndex);
                }
            }
        }

        /// <summary>
        /// Вычисляет значения пробоотборника для выбранных клеток.
        /// </summary>
        /// <param name="selectedCells">Список выбранных клеток</param>
        private void CalculateProbeSamplerValues(List<Cell> selectedCells)
        {
            foreach (var cell in selectedCells)
            {
                bool isNeedRotationOver90Degrees = cell.X > ConnectionConfiguration?.SamplerConnection.UnreachablePixels;

                if (cell.Rotation is null)
                {
                    this.Log().Info($"Calc Rotation Y: {cell.Y}; a: {ConnectionConfiguration?.SamplerConnection.RotationCalculationCoeffA}; b: {ConnectionConfiguration?.SamplerConnection.RotationCalculationCoeffB}; c: {ConnectionConfiguration?.SamplerConnection.RotationCalculationCoeffC}; isOver90Degrees: {isNeedRotationOver90Degrees}");
                    cell.Rotation = ConvectorHelper.ConvertPixelsYToRotation(
                        yPixels: cell.Y,
                        a: ConnectionConfiguration?.SamplerConnection.RotationCalculationCoeffA,
                        b: ConnectionConfiguration?.SamplerConnection.RotationCalculationCoeffB,
                        c: ConnectionConfiguration?.SamplerConnection.RotationCalculationCoeffC,
                        isOver90Degrees: isNeedRotationOver90Degrees);
                }

                if (cell.Offset is null)
                {
                    this.Log().Info($"Calc Rotation X: {cell.X}; a: {ConnectionConfiguration?.SamplerConnection.LinearCalculationCoeffA}; b: {ConnectionConfiguration?.SamplerConnection.LinearCalculationCoeffB}; CorrectionValue: {ConvectorHelper.CalculateCorrectionValue(rotation: cell.Rotation.Value, beakCoeff: ConnectionConfiguration?.SamplerConnection.BeakCoeff)}");
                    cell.Offset = ConvectorHelper.ConvertPixelsXToLinearDisplacement(
                        xPixels: cell.X,
                        a: ConnectionConfiguration?.SamplerConnection.LinearCalculationCoeffA,
                        b: ConnectionConfiguration?.SamplerConnection.LinearCalculationCoeffB,
                        CorrectionValue: ConvectorHelper.CalculateCorrectionValue(
                            rotation: cell.Rotation.Value,
                            beakCoeff: ConnectionConfiguration?.SamplerConnection.BeakCoeff),
                        isOver90Degrees: isNeedRotationOver90Degrees);
                }
            }
        }

        /// <summary>
        /// Находит кратчайший путь для выбранных клеток.
        /// </summary>
        /// <param name="selectedCells">Список выбранных клеток</param>
        /// <returns>Список клеток, упорядоченный по кратчайшему пути</returns>
        private List<Cell> FindShortestPath(List<Cell> selectedCells)
        {
            // расчет кратчайшего пути
            //var pathFinder = new ShortestPathFinder(
            //    SelectedCells,
            //    linearSpeed: OffsetVelocity.Value,
            //    rotationSpeed: RotationVelocity.Value
            //    );

            //var ShortestPathCells = pathFinder.FindShortestPathORTools();

            //ShortestPathCells = pathFinder.FindShortestPathDijkstra();
            return selectedCells.OrderBy(cell => cell.Offset).ToList();
        }

        /// <summary>
        /// Логирует координаты выбранных клеток.
        /// </summary>
        /// <param name="selectedCells">Список выбранных клеток</param>
        private void LogSelectedCells(List<Cell> selectedCells)
        {
            var cellCoordsLogMessage = $"Selected cells coords:{Environment.NewLine}";
            var cellsCoords = selectedCells.Select((cell, index) =>
                $"{index} px({Math.Round(cell.X)};{Math.Round(cell.Y)}); offset {cell.Offset}; rotation {cell.Rotation}");
            cellCoordsLogMessage += string.Join(Environment.NewLine, cellsCoords);
            this.Log().Info(cellCoordsLogMessage);
        }

        /// <summary>
        /// Записывает вычисленные значения в OPC сервер.
        /// </summary>
        /// <param name="selectedCells">Список выбранных клеток</param>
        /// <param name="probesNumber">Количество проб</param>
        /// <returns>Задача, представляющая асинхронную операцию</returns>
        private async Task WriteValuesToOpc(List<Cell> selectedCells, byte probesNumber)
        {
            int probesCounter = 1;

            foreach (var cell in selectedCells)
            {
                await opcUaClient.WriteValue($"ns=1;s=SIEMENSPLC.siemensplc.pos mast {probesCounter}", cell.Offset);
                await opcUaClient.WriteValue($"ns=1;s=SIEMENSPLC.siemensplc.pos kopf {probesCounter}", cell.Rotation);
                probesCounter++;
            }

            await opcUaClient.WriteValue("ns=1;s=SIEMENSPLC.siemensplc.anzahl_positiionen", probesNumber);
            IsPositionValuesSet = true;
        }

        /// <summary>
        /// Обновляет статус пробоотборника и логирует действие.
        /// </summary>
        /// <param name="probesNumber">Количество проб</param>
        /// <returns>Задача, представляющая асинхронную операцию</returns>
        private async Task UpdateSamplerStatusAndLog(byte probesNumber)
        {
            if (IsSampleAvailable)
            {
                await opcUaClient.WriteValue("ns=1;s=SIEMENSPLC.siemensplc.aktionen", 1);
                opcUaClient.SpsStatusValue.OnNext(SpsStatus.InProgress);
            }
        }

        /// <summary>
        /// Отключает режим ручного выбора клеток.
        /// </summary>
        private void DisableManualSelectionMode()
        {
            isManualSelectionOn.OnNext(false);
            this.Log().Info("Values successfully transferred to the sampler, disabling manual cell selection mode");
            this.SendMessageToBus("Значения успешно переданы в пробоотборник, отключаю ручной режим");
        }

        /// <summary>
        /// Логирует значения OPC сервера.
        /// </summary>
        /// <param name="probesNumber">Количество проб</param>
        /// <returns>Задача, представляющая асинхронную операцию</returns>
        private async Task LogOpcServerValues(byte probesNumber)
        {
            try
            {
                await Task.Delay(3000);
                this.Log().Info("Opc server values" + Environment.NewLine + await opcUaClient.ReadValues(probesNumber));
            }
            catch (Exception ex)
            {
                this.Log().Error("Error while reading values and logging from OPC server", ex);
            }
        }

        /// <summary>
        /// Сохраняет скриншот для логов.
        /// </summary>
        /// <returns>Задача, представляющая асинхронную операцию</returns>
        private async Task SaveScreenshotForLogs()
        {
            try
            {
                if (await savingSemaphore.WaitAsync(0))
                {
                    try
                    {
                        IsFrameSaving.OnNext(true);

                        Mat frameToSave;
                        lock (lockObj)
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
            }
            catch (Exception ex)
            {
                this.Log().Error("Error while taking screenshot and logging from OPC server", ex);
            }
        }

        /// <summary>
        /// Обрабатывает исключения, логируя ошибку и отправляя сообщение на шину.
        /// </summary>
        /// <param name="ex">Исключение для обработки</param>
        private void HandleException(Exception ex)
        {
            this.Log().Error($"Error setting sampler controller values: {ex}");
            this.Log().Error($"Error setting sampler controller values: {ex.Data}");
            this.SendMessageToBus($"Не удалось подготовить пробоотборник к взятию пробы {ex}");
        }

        #endregion

        /// <summary>
        /// метод сохранения кадра
        /// </summary>
        /// <param name="frame"></param>
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
        /// Общая функция сохранения кадра.
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

        // private void EnableManualSelection(string password)
        // {
        //    try
        //    {
        //        var settings = applicationSettingsManager.GetSettings();

        // if (Password != null && settings != null)
        //        {
        //            if (!PasswordHasher.VerifyIdentityV3Hash(Password, settings.AdminPassword))
        //            {
        //                IsPasswordValidationSuccessfully = false;
        //                this.Log().Info($"Failed attempt to enable manual selection");
        //                this.SendMessageToBus("Неверный пароль");
        //                return;
        //            };
        //            IsPasswordValidationSuccessfully = true;
        //            this.Log().Info($"Manual selection was enabled");
        //            this.SendMessageToBus("Включен ручной режим выбора клеток");
        //        }
        //    }
        //    catch
        //    {
        //        this.Log().Error($"Error while attempt to enable manual selection");
        //        this.SendMessageToBus("Ошибка при вводе пароля");
        //        IsPasswordValidationSuccessfully = false;
        //    }
        // }
        /// <summary>
        /// отключить ручной режим отбора
        /// </summary>
        private void OffManualSelection()
        {
            if (isManualSelectionOn.Value)
            {
                isManualSelectionOn.OnNext(false);
                this.Log().Info($"Disable manual selection");
                this.SendMessageToBus("Ручной режим выбора клеток отключен");
            }
        }

        /// <summary>
        /// Метод отвечающий за включение ручного режима
        /// </summary>
        private async void ChangeSelectionMethod()
        {
            // bool isGranted = adminAccessService.IsAccessGranted;

            // isGranted = false;

            // bool isPassEntered = false;
            // if (!isGranted)
            // {
            //    isPassEntered = await ShowPasswordCheck();
            // }
            // if (!isPassEntered && !isGranted) return;                      

            bool isPassEntered = false;
            if (!isManualSelectionOn.Value)
            {
                isPassEntered = await this.RequestAdminPassword();
            }

            if (!isPassEntered && !isManualSelectionOn.Value)
            {
                return;
            }

            isManualSelectionOn.OnNext(isPassEntered);
        }

        private async void CheckCalibration()
        {
            CalibrationCirclesVisibility = true;
            DetectionSwitch = true;

            if(connectionConfiguration is not null)
            {
                foreach (var circle in Circles)
                {
                    circle.CalculateValuesForOpc(connectionConfiguration);
                }
            }
            try
            {
                for (int i = 0; i < Circles.Count; i++)
                {
                    await opcUaClient.WriteValue($"ns=1;s=SIEMENSPLC.siemensplc.pos mast {i + 1}", Circles[i].Offset);

                    await opcUaClient.WriteValue($"ns=1;s=SIEMENSPLC.siemensplc.pos kopf {i + 1}", Circles[i].Rotation);
                }
                // записываем количество точек для отбора в OPC сервер
                await opcUaClient.WriteValue("ns=1;s=SIEMENSPLC.siemensplc.anzahl_positiionen", Circles.Count);

                IsPositionValuesSet = true;

                // ставим статус пробоотборника, что он находится в процессе отбора
                if (IsSampleAvailable)
                {
                    await opcUaClient.WriteValue("ns=1;s=SIEMENSPLC.siemensplc.aktionen", 1);
                    opcUaClient.SpsStatusValue.OnNext(SpsStatus.InProgress);
                }
            }
            catch
            {
                //this.Log().Info($"values ​​passed for calibration check");
                this.SendMessageToBus("что-то пошло по одному месту");
            }

            this.Log().Info($"values ​​passed for calibration check");
            this.SendMessageToBus("Ручной режим выбора клеток отключен");
        }

        private void FixDetections()
        {
            IsDetectionsFixed = !(IsDetectionsFixed);
        }
    }
}
