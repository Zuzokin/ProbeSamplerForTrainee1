using System.Windows;

namespace ProbeSampler.WPF
{
    public partial class AppSettingsView
    {
        public AppSettingsView()
        {
            InitializeComponent();
            SetupBindings();
        }

        private void SetupBindings()
        {
            this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel, vm => vm.IsPasswordSet, v => v.btnChangePassword.Visibility, vmToViewConverterOverride: new BooleanToVisibilityTypeConverter()).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.IsPasswordSet, v => v.btnSetPassword.Visibility, (state) =>
                {
                    return state ? Visibility.Collapsed : Visibility.Visible;
                }).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.ChangePasswordCommand, v => v.btnChangePassword).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.SetPasswordCommand, v => v.btnSetPassword).DisposeWith(d);
            });
        }
    }
}
