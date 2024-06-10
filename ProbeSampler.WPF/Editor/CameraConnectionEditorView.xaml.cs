using System.Reactive.Linq;
using System.Windows;
using System.Windows.Forms;

namespace ProbeSampler.WPF
{
    public partial class CameraConnectionEditorView
    {
        readonly OpenFileDialog openFileDialog = new();

        public CameraConnectionEditorView()
        {
            InitializeComponent();

            openFileDialog.Filter = "Image files (*.jpg;*.bmp;*.png)|*.jpg;*.bmp;*.png";

            SetupBindings();
        }

        private void SetupBindings()
        {
            this.WhenActivated(d =>
            {
                this.Bind(ViewModel, vm => vm.Name, v => v.strConfigName.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.URL, v => v.strCameraURI.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.URL, v => v.strImageFilePath.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.IsDebug, v => v.tglBtnIsDebug.IsChecked).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.IsNewConfiguration, v => v.DebugControl.Visibility, vmToViewConverterOverride: new BooleanToVisibilityTypeConverter()).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.IsDebug, v => v.strCameraURI.Visibility, (isDebug) =>
                {
                    if (isDebug)
                    {
                        return Visibility.Collapsed;
                    }

                    return Visibility.Visible;
                })
                .DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.IsDebug, v => v.FileControl.Visibility, vmToViewConverterOverride: new BooleanToVisibilityTypeConverter()).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.CameraInputDimens, v => v.strCameraInputDimens.Text).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.CameraVM, v => v.CameraView.ViewModel).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.CameraVM.CameraState, v => v.btnCancel.ButtonContent, LocalizationHelper.ToDisplayText).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.CameraVM.CameraState, v => v.btnCancel.Visibility, (state) =>
                {
                    if (state == CameraState.Connecting)
                    {
                        return Visibility.Visible;
                    }

                    return Visibility.Collapsed;
                })
                .DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.CancelConnectCommand, v => v.btnCancel.Command).DisposeWith(d);
                this.BindInteraction(ViewModel, vm => vm.LoadFileConfirm,
                    context => Observable.Return(openFileDialog.ShowDialog())
                    .Do(result => context.SetOutput(result.Equals(DialogResult.OK) ? openFileDialog.FileName : string.Empty)
                )).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.OpenCalibrationViewCommand, v => v.btnCameraCalibration).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.SelectImageFileCommand, v => v.btnLoadImage).DisposeWith(d);
            });
        }
    }
}
