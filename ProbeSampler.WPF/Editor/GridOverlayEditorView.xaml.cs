using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ProbeSampler.WPF
{
    public partial class GridOverlayEditorView
    {
        private Point anchorPoint;
        private bool isDragging;

        public GridOverlayEditorView()
        {
            InitializeComponent();
            SetupBindings();
        }

        private void SetupBindings()
        {
            this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel, vm => vm.CameraVM, v => v.CameraView.ViewModel).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.CancelConnectCommand, v => v.btnCancel.Command).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.CameraVM.CameraState, v => v.btnCancel.ButtonContent, LocalizationHelper.ToDisplayText).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.GridOverlayCellHeight, v => v.stepSlider.Value).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.CameraViewHeight, v => v.intCameraViewHeight.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.CameraViewWidth, v => v.intCameraViewWidth.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.GridOverlayHeight, v => v.intGridOverlayHeight.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.GridOverlayWidth, v => v.intGridOverlayWidth.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.GridOverlayControlX, v => v.intGridOverlayPositionX.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.GridOverlayControlY, v => v.intGridOverlayPositionY.Text).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.ResetGridCommand, v => v.btnResetGrid).DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.CameraVM.CameraState, v => v.btnCancel.Visibility, (state) =>
                {
                    return state == CameraState.Connecting ? Visibility.Visible : Visibility.Collapsed;
                })
                .DisposeWith(d);

                this.WhenAnyValue(x => x.ViewModel)
                    .Where(x => x != null)
                    .Subscribe(x =>
                    {
                        ViewModel.WhenAnyValue(
                                x => x.GridOverlayControlX,
                                x => x.GridOverlayControlY,
                                x => x.GridOverlayWidth,
                                x => x.GridOverlayHeight,
                                x => x.GridOverlayCellHeight)
                            // .Where(x => x.Item1 > 0 && x.Item2 > 0 && x.Item3 > 0 && x.Item4 > 0 && x.Item5 > 0) //Обновляем оверлей при любых значениях
                            .Throttle(TimeSpan.FromMilliseconds(200))
                            .Subscribe(x =>
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    this.CanvasOverlay.EraseGridOverlay();

                                    if (x.Item1 > 0
                                        && x.Item2 > 0
                                        && x.Item3 > 0
                                        && x.Item4 > 0
                                        && x.Item5 > 0
                                    ) // Если все значения присутствуют, рисуем оверлей
                                    {
                                        this.CanvasOverlay.DrawGridOverlay(
                                            startPosition: new System.Drawing.Point((int)x.Item1, (int)x.Item2),
                                            gridSize: new System.Drawing.Size((int)x.Item3, (int)x.Item4),
                                            cellSize: x.Item5);
                                    }
                                });
                            })
                            .DisposeWith(d);
                    })
                    .DisposeWith(d);

                this.CameraView.Events().MouseDown
                    .Subscribe(e =>
                    {
                        if (e.LeftButton == MouseButtonState.Pressed)
                        {
                            anchorPoint = e.GetPosition(CanvasOverlay);
                            SelectionRectangle.Visibility = Visibility.Visible;
                            Canvas.SetLeft(SelectionRectangle, anchorPoint.X);
                            Canvas.SetTop(SelectionRectangle, anchorPoint.Y);
                            SelectionRectangle.Width = 0;
                            SelectionRectangle.Height = 0;
                            isDragging = true;
                            this.CameraView.CaptureMouse();
                        }
                    })
                    .DisposeWith(d);

                this.CameraView.Events().MouseMove
                    .Subscribe(e =>
                    {
                        if (isDragging)
                        {
                            System.Windows.Point currentPoint = e.GetPosition(this.CanvasOverlay);

                            /*                            currentPoint.X = Math.Clamp(currentPoint.X, 0, CanvasOverlay.ActualWidth);
                                                        currentPoint.Y = Math.Clamp(currentPoint.Y, 0, CanvasOverlay.ActualHeight);*/

                            var x = Math.Min(currentPoint.X, anchorPoint.X);
                            var y = Math.Min(currentPoint.Y, anchorPoint.Y);
                            var w = Math.Abs(currentPoint.X - anchorPoint.X);
                            var h = Math.Abs(currentPoint.Y - anchorPoint.Y);

                            if (x + w > CameraView.ActualWidth)
                            {
                                w = Math.Max(0, CameraView.ActualWidth - x);
                            }

                            if (y + h > CameraView.ActualHeight)
                            {
                                h = Math.Max(0, CameraView.ActualHeight - y);
                            }

                            Canvas.SetLeft(SelectionRectangle, x);
                            Canvas.SetTop(SelectionRectangle, y);
                            SelectionRectangle.Width = w;
                            SelectionRectangle.Height = h;
                        }
                    })
                    .DisposeWith(d);

                this.CameraView.Events().MouseUp
                    .Subscribe(e =>
                    {
                        if (isDragging)
                        {
                            isDragging = false;

                            double x = Canvas.GetLeft(SelectionRectangle);
                            double y = Canvas.GetTop(SelectionRectangle);
                            double w = SelectionRectangle.Width;
                            double h = SelectionRectangle.Height;
                            System.Drawing.Size gridSize = new System.Drawing.Size((int)SelectionRectangle.Width, (int)SelectionRectangle.Height);
                            this.SelectionRectangle.Visibility = Visibility.Collapsed;
                            double cellSize = stepSlider.Value;
                            this.CameraView.ReleaseMouseCapture();

                            this.ViewModel?.UpdateOverlayDimensionsCommand?
                                .Execute((height: (int)h, width: (int)w, x: x, y: y))
                                .Subscribe();
                        }
                    })
                    .DisposeWith(d);
            });
        }
    }
}
