using MaterialDesignThemes.Wpf;
using System.Reactive;

namespace ProbeSampler.Presentation
{
    public class SetAdminPasswordViewModel : ViewModelBase
    {
        private readonly IAdminAccessService adminAccessService;
        /// <summary>
        /// Новый пароль.
        /// </summary>
        [Reactive]
        public string NewPassword { get; set; } = string.Empty;
        /// <summary>
        /// Подтверждение пароля.
        /// </summary>
        [Reactive]
        public string PasswordConfirm { get; set; } = string.Empty;

        public ReactiveCommand<Unit, Unit>? SetPasswordCommand { get; private set; }

        public SetAdminPasswordViewModel(IAdminAccessService? adminAccessService = null)
        {
            this.adminAccessService = adminAccessService ?? resolver.GetRequiredService<IAdminAccessService>();
        }

        protected override void SetupCommands()
        {
            base.SetupCommands();

            SetPasswordCommand = ReactiveCommand.Create(SetPassword);
        }

        private void SetPassword()
        {
            adminAccessService.ChangeAdminPassword(NewPassword);
            DialogHost.CloseDialogCommand.Execute(true, null);
        }
    }
}
