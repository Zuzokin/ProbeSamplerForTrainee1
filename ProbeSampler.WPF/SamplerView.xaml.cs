using System.Globalization;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DynamicData;

namespace ProbeSampler.WPF
{
    public class OverlayCellBoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? new SolidColorBrush(Colors.LightGreen) : (object)new SolidColorBrush(Colors.Crimson);
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class OverlayCellStateToStrokeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CellState state)
            {
                switch (state)
                {
                    case CellState.Default:
                        return new SolidColorBrush(Colors.Crimson);
                    case CellState.Selected:
                        return new SolidColorBrush(Colors.Purple);
                    case CellState.AvailableForSelect:
                        return new SolidColorBrush(Colors.Yellow);
                }
            }
            return new SolidColorBrush(Colors.Crimson);
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    public class OverlayCellStateToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CellState state)
            {
                switch (state)
                {
                    case CellState.Default:
                        return 1;
                    case CellState.Selected:
                        return 0.7;
                    case CellState.AvailableForSelect:
                        return 1;
                }
            }
            return new SolidColorBrush(Colors.Crimson);
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    public class OverlayCellStateToFillColorConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CellState state)
            {
                switch (state)
                {
                    case CellState.Default:
                        return new SolidColorBrush(Colors.Transparent);
                    case CellState.Selected:
                        return new SolidColorBrush(Colors.Purple);
                    case CellState.AvailableForSelect:
                        return new SolidColorBrush(Colors.Transparent);
                }
            }
            return new SolidColorBrush(Colors.Crimson);
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public partial class SamplerView
    {
        public SamplerView()
        {
            InitializeComponent();
            SetupBinding();
        }

        private void SetupBinding()
        {
            this.WhenActivated(d =>
            {
                this.Bind(ViewModel, vm => vm.DetectionSwitch, v => v.tglBtnDetectionType.IsChecked).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.GridOverlayVisibility, v => v.tglBtnOverlayVisib.IsChecked).DisposeWith(d); 
                this.Bind(ViewModel, vm => vm.ManualCellsSelection, v => v.tglBtnManualCellsSelection.IsChecked).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.DetectionType, v => v.DetectionTypeCaption.Text, LocalizationHelper.ToDisplayText).DisposeWith(d); 
                this.OneWayBind(ViewModel, vm => vm.GridOverlayVisibility, v => v.CanvasOverlay.Visibility, vmToViewConverterOverride: new BooleanToVisibilityTypeConverter()).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.CameraVM, v => v.CameraView.ViewModel).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.CanCamOpera, v => v.CanvasOverlay.IsEnabled).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.CanCamOpera, v => v.OverlayControl.IsEnabled).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.CanCamOpera, v => v.DetectionControl.IsEnabled).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.CameraVM.CameraState, v => v.strCameraStatus.Text, LocalizationHelper.ToDisplayText).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.OPCUAConnectionStatus, v => v.strOPCStatus.Text, LocalizationHelper.ToDisplayText).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.CancelConnectCommand, v => v.btnCancel.Command).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.CameraVM.CameraState, v => v.btnCancel.ButtonContent, LocalizationHelper.ToDisplayText).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.OPCUAConnectionStatus, v => v.btnCancel.ButtonContent, LocalizationHelper.ToDisplayText).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.StartCommand, v => v.btnStart).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.StopCommand, v => v.btnStop).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.ConnectCommand, v => v.btnConnect).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.PauseCommand, v => v.btnPause).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.SavePreprocessedFrameCommand, v => v.btnSavePreprocessedFrame).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.SelectRandomCellsCommand, v => v.btnSelectRandomCells).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.ResetSelectedCellsCommand, v => v.btnResetSelectedCells).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.OpcConnectCommand, v => v.btnOpcConnect).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.OpcDisconnectCommand, v => v.btnOpcDisconnect).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.OpcTakeProbeCommand, v => v.btnOpcTakeProbe).DisposeWith(d); 
                this.BindCommand(ViewModel, vm => vm.EnableManualSelectionCommand, v => v.btnEnableManualSelection).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.OffManualSelectionCommand, v => v.btnOffManualSelection).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.ChangeSelectionMethodCommand, v => v.btnChangeSelection).DisposeWith(d);
                this.WhenAnyValue(x => x.ViewModel)
                    .Subscribe(x =>
                    {
                        this.WhenAnyValue(x => x.ViewModel).BindTo(this, x => x.DataContext);
                    })
                    .DisposeWith(d);

                this.WhenAnyValue(x => x.ViewModel)
                    .Where(x => x != null)
                    .Subscribe(x =>
                    {
                        if (ViewModel == null)
                            return;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            this.CanvasOverlay.EraseGridOverlay();
                            foreach (var cell in ViewModel.Cells)
                            {
                                var rect = this.CanvasOverlay.DrawCellOverlay(cell);
                                rect.MouseDown += Rect_MouseDown;
                                rect.DataContext = cell;
                                var strokeColorBinding = new Binding(nameof(Cell.CellState))
                                {
                                    Source = cell,
                                    Converter = new OverlayCellStateToStrokeColorConverter(),
                                };
                                var fillColorBinding = new Binding(nameof(Cell.CellState))
                                {
                                    Source = cell,
                                    Converter = new OverlayCellStateToFillColorConverter(),
                                };
                                var opacityBinding = new Binding(nameof(Cell.CellState))
                                {
                                    Source = cell,
                                    Converter = new OverlayCellStateToOpacityConverter(),
                                };
                                rect.SetBinding(Rectangle.StrokeProperty, strokeColorBinding);
                                rect.SetBinding(Rectangle.FillProperty, fillColorBinding);
                                rect.SetBinding(Rectangle.OpacityProperty, opacityBinding);
                            }
                        });
                    })
                    .DisposeWith(d);

                this.OneWayBind(ViewModel, vm => vm.OPCUAConnectionStatus, v => v.btnCancel.Visibility, (state) =>
                {
                    return state == OPCUAConnectionStatus.Connecting ? Visibility.Visible : Visibility.Collapsed;
                });
                this.OneWayBind(ViewModel, vm => vm.CameraVM.CameraState, v => v.btnCancel.Visibility, (state) =>
                {
                    return state == CameraState.Connecting ? Visibility.Visible : Visibility.Collapsed;
                });                
                this.OneWayBind(ViewModel, vm => vm.ManualCellsSelection, v => v.strPassword.Visibility, (state) =>
                {
                    return state ? Visibility.Visible : Visibility.Collapsed;
                });
                this.OneWayBind(ViewModel, vm => vm.ManualCellsSelection, v => v.btnEnableManualSelection.Visibility, (state) =>
                {
                    return state ? Visibility.Visible : Visibility.Collapsed;
                });
                this.OneWayBind(ViewModel, vm => vm.isPasswordValidationSuccessfully, v => v.textBoxX.Visibility, (state) =>
                {
                    return state ? Visibility.Visible : Visibility.Collapsed;
                });                
                this.OneWayBind(ViewModel, vm => vm.isPasswordValidationSuccessfully, v => v.textBoxY.Visibility, (state) =>
                {
                    return state ? Visibility.Visible : Visibility.Collapsed;
                });
                this.OneWayBind(ViewModel, vm => vm.isPasswordValidationSuccessfully, v => v.labelManualCellsSelection.Visibility, (state) =>
                {
                    return state ? Visibility.Visible : Visibility.Collapsed;
                });
                this.OneWayBind(ViewModel, vm => vm.isPasswordValidationSuccessfully, v => v.btnOffManualSelection.Visibility, (state) =>
                {
                    return state ? Visibility.Visible : Visibility.Collapsed;
                });
                this.OneWayBind(ViewModel, vm => vm.ManualCellsSelection, v => v.strPassword.Password, (status) =>
                {
                    return (!status) ? string.Empty : strPassword.Password;
                });
            });
        }

        private void Rect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Rectangle? rectangle = sender as Rectangle;
            if (rectangle?.DataContext is Cell cell && btnOffManualSelection.Visibility == Visibility.Visible)
            {
                CellsHelper.UpdateCell(cell);
                textBoxX.Text = Convert.ToString(cell.X);
                textBoxY.Text = Convert.ToString(cell.Y);
            }
            rectangle?.UpdateLayout();
        }

        class CellItem
        {
            public bool IsSelected { get; set; }
            public int Row { get; set; }
            public int Column { get; set; }
        }
    }
}
