using DynamicData;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using ProbeSampler.Core.Extensions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace ProbeSampler.Presentation
{
    public enum CalibrationViewMode
    {
        Start,
        Creation,
        Confirm,
    }
    // todo добавить возможность перейти в окно калибровки не выбрав ни одного фото
    public class CalibrationViewModel : RoutableViewModelBase, IEnableLogger
    {
        private readonly IConnectionConfigurationManager manager;
        private readonly Interaction<Unit, IEnumerable<string>> loadFileConfirm = new();
        private List<VectorOfPointF> allCorners = new();
        private List<MCvPoint3D32f[]> allPoints = new();
        private List<double> imageScales = new();
        private CancellationTokenSource cancellationTokenSource = new();
        private Mat cameraMatrix = new(3, 3, DepthType.Cv64F, 1);
        private Mat distCoeffs = new(8, 1, DepthType.Cv64F, 1);
        private Mat[]? rvecs, tvecs;
        private UndistortProcessor? undistortProcessor;
        private ImageRotationProcessor? imageRotationProcessor;

        public CameraViewModel CameraVM { get; private set; }

        public Interaction<Unit, IEnumerable<string>> LoadFileConfirm => loadFileConfirm;

        public SourceCache<ImageData, Guid> SourceCacheImageData { get; private set; } = new SourceCache<ImageData, Guid>(t => t.Id);

        public ReactiveCommand<Unit, Unit>? LoadImageFilesCommand { get; private set; }

        public ReactiveCommand<Guid, Unit>? LoadExistCalibrationCommand { get; private set; }

        public ReactiveCommand<Unit, Unit>? ProceedToCalibrationCommand { get; private set; }

        public ReactiveCommand<Unit, Unit>? GoBackCommand { get; private set; }

        public ReactiveCommand<Unit, Unit>? ClearLoadedImagesCommand { get; private set; }

        public ReactiveCommand<Unit, Unit>? ConfirmCoefChangesCommand { get; private set; }

        public ReactiveCommand<Unit, Unit>? CancelImageLoadingCommand { get; private set; }

        public ReactiveCommand<Unit, Unit>? StartCommand { get; private set; }

        public ReactiveCommand<Unit, Unit>? StopCommand { get; private set; }

        public ReactiveCommand<Unit, Unit>? ConnectCommand { get; private set; }

        public ReactiveCommand<Unit, Unit>? CancelConnectCommand { get; private set; }

        public ReactiveCommand<Unit, Unit>? PauseCommand { get; private set; }

        public ObservableCollection<CameraConnection> ExistCalibrations { get; } = new ObservableCollection<CameraConnection>();

        [ObservableAsProperty] public bool CanLoad { get; }

        [Reactive] private ConnectionConfiguration ConnectionConfiguration { get; set; }

        [Reactive] public CalibrationViewMode ViewMode { get; private set; }

        [ObservableAsProperty] public Size PatternSize { get; }

        [Reactive] public double RotationValue { get; private set; }

        [Reactive] public int PatternWidth { get; private set; } = 5;

        [Reactive] public int PatternHeight { get; private set; } = 7;

        [Reactive] public float SquareSize { get; private set; } = 140;

        [Reactive] public bool IsCurrentlyLoading { get; private set; }

        [Reactive] public double Alpha { get; private set; }

        [Reactive] public double[,]? CameraMatrix { get; private set; }

        [Reactive] public double[,]? DistCoeff { get; private set; }

        [Reactive] public double Scale { get; private set; }

        // calibration coefs
        [Reactive] public double FxCoef { get; private set; } = 0;

        [Reactive] public double FyCoef { get; private set; } = 0;

        [Reactive] public double CxCoef { get; private set; } = 0;

        [Reactive] public double CyCoef { get; private set; } = 0;

        [Reactive] public double K1Coef { get; private set; } = 0;

        [Reactive] public double K2Coef { get; private set; } = 0;

        [Reactive] public double K3Coef { get; private set; } = 0;

        [Reactive] public double P1Coef { get; private set; } = 0;

        [Reactive] public double P2Coef { get; private set; } = 0;

        public CalibrationViewModel(ConnectionConfiguration connectionConfiguration)
        {
            manager = resolver.GetRequiredService<IConnectionConfigurationManager>();
            CameraVM = resolver.GetRequiredService<CameraViewModel>();

            ConnectionConfiguration = connectionConfiguration;
            CameraVM.IsDebug = connectionConfiguration.IsDebug;

            var exitsCameraCalibrations =
                manager.SourceCacheConnectionConfigurations.Items
                .Where(x =>
                    x.CameraConnection?.CameraMatrix != null &&
                    x.CameraConnection?.DistCoeff != null)
                .Select(x => x.CameraConnection);

            ExistCalibrations.AddRange(exitsCameraCalibrations);

            ViewMode = CalibrationViewMode.Creation;

            /*            if (ExistCalibrations.Count == 0)
                            ViewMode = CalibrationViewMode.Creation;*/
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
                    connection.WhenAnyValue(x => x.Name)
                        .Select(name => $"Калибровка {name}")
                        .ToPropertyEx(this, x => x.ViewName)
                        .DisposeWith(d);

                    this.WhenAnyValue(x => x.DistCoeff)
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Where(x => x != null)
                        .Subscribe(distCoeff =>
                        {
                            if (ConnectionConfiguration.CameraConnection != null)
                            {
                                ConnectionConfiguration.CameraConnection.DistCoeff = distCoeff;
                                // todo добавить проверку на null? а точно здесь надо .ObserveOn(RxApp.MainThreadScheduler), без этого ошибка
                                // Вызывающий поток не может получить доступ к данному объекту, так как владельцем этого объекта является другой поток.
                                K1Coef = distCoeff[0, 0];
                                K2Coef = distCoeff[1, 0];
                                K3Coef = distCoeff[4, 0];
                                P1Coef = distCoeff[2, 0];
                                P2Coef = distCoeff[3, 0];
                            }
                        })
                        .DisposeWith(d);
                    this.WhenAnyValue(x => x.CameraMatrix)
                        .ObserveOn(RxApp.MainThreadScheduler)
                        .Where(x => x != null)
                        .Subscribe(cameraMatrix =>
                        {
                            if (ConnectionConfiguration.CameraConnection != null)
                            {
                                ConnectionConfiguration.CameraConnection.CameraMatrix = cameraMatrix;

                                // todo добавить проверку на null?
                                FxCoef = cameraMatrix[0, 0];
                                FyCoef = cameraMatrix[1, 1];
                                CxCoef = cameraMatrix[0, 2];
                                CyCoef = cameraMatrix[1, 2];
                            }
                        })
                        .DisposeWith(d);
                    this.WhenAnyValue(x => x.Alpha)
                        .Where(x => x >= 0 && x <= 1)
                        .Subscribe(alpha =>
                        {
                            if (ConnectionConfiguration.CameraConnection != null)
                            {
                                ConnectionConfiguration.CameraConnection.Alpha = alpha;
                            }
                        })
                        .DisposeWith(d);
                    this.WhenAnyValue(x => x.RotationValue)
                        .Subscribe(rotation =>
                        {
                            if (ConnectionConfiguration.CameraConnection != null)
                            {
                                ConnectionConfiguration.CameraConnection.RotationAngle = rotation;
                            }
                        })
                        .DisposeWith(d);
                    this.WhenAnyValue(x => x.Scale)
                        .Where(x => x > 0)
                        .Subscribe(scale =>
                        {
                            if (ConnectionConfiguration.CameraConnection != null)
                            {
                                ConnectionConfiguration.CameraConnection.Scale = scale;
                            }
                        })
                        .DisposeWith(d);
                })
                .DisposeWith(d);

            this.WhenAnyValue(x => x.PatternWidth, x => x.PatternHeight)
                .Where(x => x.Item1 > 0 && x.Item2 > 0)
                .Select(x => new Size(x.Item1, x.Item2))
                .ToPropertyEx(this, x => x.PatternSize)
                .DisposeWith(d);

            this.WhenAnyValue(x => x.PatternSize, x => x.SquareSize)
                .Select(values => values.Item1.Width > 0 && values.Item1.Height > 0 && values.Item2 > 0)
                .ToPropertyEx(this, x => x.CanLoad)
                .DisposeWith(d);

            this.WhenAnyValue(x => x.ViewMode)
                .Where(x => x == CalibrationViewMode.Confirm)
                .Subscribe(mode =>
                {
                    CameraVM.ImageProcessingPipeline += CameraVM_ImageProcessingPipeline;
                    ConnectCommand?.Execute()
                        .Subscribe(x =>
                        {
                            CameraVM.WhenAnyValue(x => x.FrameHeight, x => x.FrameWidth)
                                .Where(x => x.Item1 > 0 && x.Item2 > 0)
                                .Throttle(TimeSpan.FromMilliseconds(100))
                                .Subscribe(async x =>
                                {
                                    var messageBoxResult = System.Windows.MessageBox.Show("Выполнить автоматическую калибровку?", "Автоматическая калибровка", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                                    if (messageBoxResult == System.Windows.MessageBoxResult.Yes)
                                        await CalibrateCamera();
                                    else
                                    {
                                        if (undistortProcessor != null && ConnectionConfiguration.CameraConnection != null)
                                        {
                                            Alpha = ConnectionConfiguration.CameraConnection.Alpha;
                                            CameraMatrix = ConnectionConfiguration.CameraConnection.CameraMatrix;
                                            DistCoeff = ConnectionConfiguration.CameraConnection.DistCoeff;
                                            RotationValue = ConnectionConfiguration.CameraConnection.RotationAngle;
                                        }
                                    }
                                })
                                .DisposeWith(d);
                        })
                        .DisposeWith(d);
                })
                .DisposeWith(d);

            this.WhenAnyValue(x => x.Alpha)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Subscribe(alpha =>
                {
                    if (undistortProcessor != null)
                    {
                        undistortProcessor.Alpha = alpha;
                    }
                })
                .DisposeWith(d);

            this.WhenAnyValue(x => x.RotationValue)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Subscribe(rotation =>
                {
                    if (imageRotationProcessor != null)
                    {
                        imageRotationProcessor.Angle = RotationValue;
                    }
                })
                .DisposeWith(d);

            this.WhenAnyValue(x => x.ViewMode)
                .Buffer(2, 1)
                .Where(buffer => buffer[0] == CalibrationViewMode.Confirm && buffer[1] == CalibrationViewMode.Creation)
                .Subscribe(_ =>
                {
                    CameraVM.ImageProcessingPipeline -= CameraVM_ImageProcessingPipeline;
                    StopCommand?.Execute().Subscribe();
                })
                .DisposeWith(d);
        }

        protected override void SetupCommands()
        {
            base.SetupCommands();

            var canLoad = this.WhenAnyValue(x => x.CanLoad)
                .Select(x => x);

            var canProceedToCalibration = SourceCacheImageData
                .Connect()
                .ToCollection()
                .Select(items => items.Any(item => item.IsPatternFounded))
                .DistinctUntilChanged()
                .StartWith(false)
                .ObserveOn(RxApp.MainThreadScheduler);

            LoadImageFilesCommand = ReactiveCommand.Create(LoadFiles, canLoad);

            ProceedToCalibrationCommand = ReactiveCommand.Create(ProceedToCalibration, canProceedToCalibration);

            LoadExistCalibrationCommand = ReactiveCommand.Create<Guid, Unit>(LoadExistCalibration);

            /*            var existCalibrationsCountChanged = Observable
                            .FromEventPattern<NotifyCollectionChangedEventArgs>(ExistCalibrations, nameof(ExistCalibrations.CollectionChanged))
                            .Select(_ => ExistCalibrations.Count);*/

            /*            var canGoBack = this.WhenAnyValue(x => x.ViewMode)
                            .CombineLatest(existCalibrationsCountChanged.StartWith(ExistCalibrations.Count),
                                (viewMode, count) =>
                                (viewMode != CalibrationViewMode.Start) &&
                                !(viewMode == CalibrationViewMode.Confirm && count == 0)
                            );*/

            var canGoBack = this.WhenAnyValue(x => x.ViewMode)
                .Select(x => x != CalibrationViewMode.Start && x != CalibrationViewMode.Creation);

            GoBackCommand = ReactiveCommand.Create(GoBack, canGoBack);

            var canClear = SourceCacheImageData
                .Connect()
                .ToCollection()
                .Select(items => items.Count > 0)
                .DistinctUntilChanged()
                .StartWith(false)
                .ObserveOn(RxApp.MainThreadScheduler);

            ClearLoadedImagesCommand = ReactiveCommand.Create(ClearImages, canClear);

            CancelImageLoadingCommand = ReactiveCommand.Create(CancelLoading);

            var cancelSubject = new Subject<Unit>();

            var canConnect = CameraVM.WhenAnyValue(x => x.CameraState)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Select(x => x == CameraState.Stopped || x == CameraState.Error);

            ConnectCommand = ReactiveCommand.CreateFromObservable(
                () =>
                Observable.StartAsync(ct => CameraVM.Connect(ConnectionConfiguration.CameraConnection?.URL ?? "", ct))
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

            ConfirmCoefChangesCommand = ReactiveCommand.Create(ConfirmCoefChanges);
        }

        private void ProceedToCalibration()
        {
            ViewMode = CalibrationViewMode.Confirm;
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

            return output;
        }

        private void LoadFiles()
        {
            CancellationToken ct = cancellationTokenSource.Token;

            loadFileConfirm
                .Handle(Unit.Default)
                .Subscribe(async files =>
                {
                    IsCurrentlyLoading = true;

                    await Task.Run(
                        () =>
                    {
                        try
                        {
                            ct.ThrowIfCancellationRequested();

                            foreach (string file in files)
                            {
                                ct.ThrowIfCancellationRequested();

                                try
                                {
                                    Mat inputImage = CvInvoke.Imread(file, ImreadModes.Color);
                                    var grayImage = new UMat();
                                    CvInvoke.CvtColor(inputImage, grayImage, ColorConversion.Bgr2Gray);
                                    var corners = new VectorOfPointF();
                                    bool found = CvInvoke.FindChessboardCorners(grayImage, PatternSize, corners);

                                    if (found)
                                    {
                                        CvInvoke.CornerSubPix(grayImage, corners, new Size(11, 11), new Size(-1, -1), new MCvTermCriteria(30, 0.01));
                                        allCorners.Add(corners);

                                        MCvPoint3D32f[] objectPoints = new MCvPoint3D32f[PatternSize.Width * PatternSize.Height];
                                        for (int j = 0; j < PatternSize.Height; j++)
                                        {
                                            for (int k = 0; k < PatternSize.Width; k++)
                                            {
                                                objectPoints[(j * PatternSize.Width) + k] = new MCvPoint3D32f(j * SquareSize, k * SquareSize, 0);
                                            }
                                        }

                                        allPoints.Add(objectPoints);

                                        imageScales.Add(ComputeScale(corners, SquareSize));
                                    }

                                    var fileName = Path.GetFileName(file);
                                    Guid guid = Guid.NewGuid();
                                    SourceCacheImageData.AddOrUpdate(new ImageData { Id = guid, Name = fileName, ImageMat = grayImage, IsPatternFounded = found });
                                }
                                catch (Exception ex)
                                {
                                    this.Log().Error($"Error with file {file}: {ex}");
                                }
                            }

                            if (imageScales.Count > 0)
                            {
                                Scale = Math.Round(imageScales.Average(), 2);
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            this.Log().Debug("LoadFiles was canceled");
                            ClearImages();
                        }
                    }, ct);

                    IsCurrentlyLoading = false;
                });
        }

        private Unit LoadExistCalibration(Guid id)
        {
            return Unit.Default;
        }

        private void GoBack()
        {
            ViewMode--;
        }

        private void ClearImages()
        {
            SourceCacheImageData.Clear();
            allCorners.Clear();
            allPoints.Clear();
            imageScales.Clear();
        }

        private void CancelLoading()
        {
            cancellationTokenSource?.Cancel();
        }

        private async Task CalibrateCamera()
        {
            PointF[][] imagePoints = allCorners.Select(c => c.ToArray()).ToArray();
            var termCriteria = new MCvTermCriteria(maxIteration: 1000, eps: 1e-5);

            await Task.Run(() =>
            {
                double calibrationError = CvInvoke.CalibrateCamera(
                    objectPoints: allPoints.ToArray(),
                    imagePoints: imagePoints.ToArray(),
                    imageSize: /*new Size(ConnectionConfiguration.CameraConnection.InputHeight,
                                        ConnectionConfiguration.CameraConnection.InputWidth),*/
                    CameraVM.FrameSize,
                    cameraMatrix: cameraMatrix,
                    distortionCoeffs: distCoeffs,
                    calibrationType: CalibType.Default,
                    termCriteria: termCriteria,
                    rotationVectors: out rvecs,
                    translationVectors: out tvecs
                  );
            });

            var cameraArray = cameraMatrix.ToArray();
            var distCoefArray = distCoeffs.ToArray();

            CameraMatrix = cameraArray;
            DistCoeff = distCoefArray;

            if (undistortProcessor != null)
            {
                undistortProcessor.CameraMatrixArray = cameraArray;
                undistortProcessor.DistCoeffsArray = distCoefArray;
            }
        }

        private void ConfirmCoefChanges()
        {
            if (undistortProcessor != null)
            {
                CameraMatrix = new double[,] {
                    { FxCoef, 0.0, CxCoef },
                    { 0.0, FyCoef, CyCoef },
                    { 0.0, 0.0, 1.0 },
                };

                DistCoeff = new double[,] {
                    { K1Coef }, { K2Coef }, { P1Coef }, { P2Coef }, { K3Coef },
                };

                undistortProcessor.CameraMatrixArray = CameraMatrix;
                undistortProcessor.DistCoeffsArray = DistCoeff;
            }
        }

        private double ComputeScale(VectorOfPointF corners, double realCellSize)
        {
            // Вычисляем расстояние в пикселях между двумя углами
            PointF corner1 = corners[0];
            PointF corner2 = corners[1];
            double pixelDistance = Math.Sqrt(Math.Pow(corner2.X - corner1.X, 2) + Math.Pow(corner2.Y - corner1.Y, 2));

            // Вычисляем соотношение между пикселями и реальными единицами
            double scale = realCellSize / pixelDistance;

            return scale; // возвращает единицы на пиксель (например, мм/пиксель)
        }

        public class ImageData
        {
            public Guid Id { get; set; }

            public UMat? ImageMat { get; set; }

            public string? Name { get; set; }

            public bool IsPatternFounded { get; set; }
        }
    }
}
