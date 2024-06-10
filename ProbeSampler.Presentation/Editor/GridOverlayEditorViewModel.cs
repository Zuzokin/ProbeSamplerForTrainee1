using System.Reactive;

namespace ProbeSampler.Presentation
{
    public class GridOverlayEditorViewModel : ViewModelBase
    {
        [Reactive] public CameraViewModel CameraVM { get; set; }

        [Reactive] public ConnectionConfiguration ConnectionConfiguration { get; set; }

        [Reactive] public int GridOverlayCellHeight { get; set; } = 10;

        [Reactive] public int GridOverlayRowCount { get; set; }

        [Reactive] public int GridOverlayColumnCount { get; set; }

        [Reactive] public double GridOverlayControlX { get; set; }

        [Reactive] public double GridOverlayControlY { get; set; }

        [Reactive] public double CameraViewHeight { get; set; }

        [Reactive] public double CameraViewWidth { get; set; }

        [Reactive] public double GridOverlayWidth { get; set; }

        [Reactive] public double GridOverlayHeight { get; set; }

        public ReactiveCommand<Unit, Unit>? CancelConnectCommand { get; set; }

        public ReactiveCommand<(int height, int width, double x, double y), Unit>? UpdateOverlayDimensionsCommand { get; set; }

        public ReactiveCommand<Unit, Unit>? ResetGridCommand { get; set; }

        public GridOverlayEditorViewModel(CameraViewModel cameraVM, ConnectionConfiguration connectionConfiguration)
        {
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
                if (ConnectionConfiguration?.CameraConnection != null)
                {
                    CameraViewHeight = ConnectionConfiguration.CameraConnection.ViewHeight;
                    CameraViewWidth = ConnectionConfiguration.CameraConnection.ViewWidth;
                    GridOverlayHeight = ConnectionConfiguration.CameraConnection.GridOverlayHeight;
                    GridOverlayWidth = ConnectionConfiguration.CameraConnection.GridOverlayWidth;
                    GridOverlayControlX = ConnectionConfiguration.CameraConnection.GridOverlayX;
                    GridOverlayControlY = ConnectionConfiguration.CameraConnection.GridOverlayY;
                    GridOverlayCellHeight = ConnectionConfiguration.CameraConnection.GridOverlayCellHeight;
                }

                this.WhenAnyValue(x => x.CameraViewHeight)
                    .Subscribe(viewHeight =>
                    {
                        if (ConnectionConfiguration?.CameraConnection != null)
                        {
                            ConnectionConfiguration.CameraConnection.ViewHeight = viewHeight;
                        }
                    })
                    .DisposeWith(d);

                this.WhenAnyValue(x => x.CameraViewWidth)
                    .Subscribe(viewWidth =>
                    {
                        if (ConnectionConfiguration?.CameraConnection != null)
                        {
                            ConnectionConfiguration.CameraConnection.ViewWidth = viewWidth;
                        }
                    })
                    .DisposeWith(d);

                this.WhenAnyValue(x => x.GridOverlayHeight)
                    .Subscribe(overlayHeight =>
                    {
                        if (ConnectionConfiguration?.CameraConnection != null)
                        {
                            ConnectionConfiguration.CameraConnection.GridOverlayHeight = overlayHeight;
                        }
                    })
                    .DisposeWith(d);

                this.WhenAnyValue(x => x.GridOverlayWidth)
                    .Subscribe(overlayWidth =>
                    {
                        if (ConnectionConfiguration?.CameraConnection != null)
                        {
                            ConnectionConfiguration.CameraConnection.GridOverlayWidth = overlayWidth;
                        }
                    })
                    .DisposeWith(d);

                this.WhenAnyValue(x => x.GridOverlayControlX)
                    .Subscribe(overlayX =>
                    {
                        if (ConnectionConfiguration?.CameraConnection != null)
                        {
                            ConnectionConfiguration.CameraConnection.GridOverlayX = overlayX;
                        }
                    })
                    .DisposeWith(d);

                this.WhenAnyValue(x => x.GridOverlayControlY)
                    .Subscribe(overlayY =>
                    {
                        if (ConnectionConfiguration?.CameraConnection != null)
                        {
                            ConnectionConfiguration.CameraConnection.GridOverlayY = overlayY;
                        }
                    })
                    .DisposeWith(d);

                this.WhenAnyValue(x => x.GridOverlayCellHeight)
                    .Subscribe(cellHeight =>
                    {
                        if (ConnectionConfiguration?.CameraConnection != null)
                        {
                            ConnectionConfiguration.CameraConnection.GridOverlayCellHeight = cellHeight;
                        }
                    })
                    .DisposeWith(d);
            })
            .DisposeWith(d);

            this.WhenAnyValue(x => x.GridOverlayCellHeight, x => x.GridOverlayHeight, x => x.GridOverlayWidth)
                .Where(x => x.Item1 > 10 && x.Item2 > 0 && x.Item3 > 0)
                .Subscribe(x =>
                {
                    GridOverlayRowCount = (int)GridOverlayHeight / GridOverlayCellHeight;
                    GridOverlayColumnCount = (int)GridOverlayWidth / GridOverlayCellHeight;
                })
                .DisposeWith(d);

            this.WhenAnyValue(x => x.GridOverlayRowCount, x => x.GridOverlayColumnCount)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .Where(x => x.Item1 > 0 && x.Item2 > 0)
                .Subscribe(async _ =>
                {
                    // todo add implement async method
                    // await UpdateCellsAsync();
                })
                .DisposeWith(d);
        }

        protected override void SetupCommands()
        {
            base.SetupCommands();

            UpdateOverlayDimensionsCommand = ReactiveCommand.Create<(int, int, double, double)>(dimensions => UpdateOverlayDimensions(dimensions));
            ResetGridCommand = ReactiveCommand.Create(ResetGrid);
        }

        private Unit UpdateOverlayDimensions((int height, int width, double x, double y) dimensions)
        {
            GridOverlayControlX = dimensions.x;
            GridOverlayControlY = dimensions.y;
            GridOverlayWidth = dimensions.width;
            GridOverlayHeight = dimensions.height;
            return Unit.Default;
        }

        private Unit ResetGrid()
        {
            GridOverlayControlX = 0;
            GridOverlayControlY = 0;
            GridOverlayWidth = 0;
            GridOverlayHeight = 0;
            return Unit.Default;
        }
    }
}
