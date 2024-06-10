namespace ProbeSampler.WPF
{
    public partial class ConfigEditorView
    {
        public ConfigEditorView()
        {
            InitializeComponent();
            SetupBindings();
        }

        private void SetupBindings()
        {
            this.WhenActivated(d =>
            {
                /*                this.OneWayBind(ViewModel, vm => vm.CameraBusy, v => v.strCameraURI.IsEnabled,
                                    (b) => { return !b; }).DisposeWith(d);*/
                this.OneWayBind(ViewModel, vm => vm.CameraConnectionVM, v => v.CameraConnectionView.ViewModel).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.GridOverlayVM, v => v.GridOverlayView.ViewModel).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.SamplerConnectionVM, v => v.SamplerConnectionView.ViewModel).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.StartCommand, v => v.btnStart).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.StopCommand, v => v.btnStop).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.ConnectCommand, v => v.btnConnect).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.PauseCommand, v => v.btnPause).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.SaveCommand, v => v.btnSave).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.CancelCommand, v => v.btnCancel).DisposeWith(d);
                // this.BindCommand(ViewModel, vm => vm.LoadImageFilesCommand, v => v.btnLoadImage).DisposeWith(d);
            });
        }
    }
}
