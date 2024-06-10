using MaterialDesignThemes.Wpf;
using ReactiveUI.Validation.Extensions;
using System.Reactive;
using System.Reactive.Subjects;

namespace ProbeSampler.Presentation
{
    public class ChangeAdminPasswordViewModel : ViewModelBase
    {
        private readonly IAdminAccessService adminAccessService;
        private BehaviorSubject<bool> isCurrentPasswordValid = new BehaviorSubject<bool>(true);
        /// <summary>
        /// Текущий пароль.
        /// </summary>
        [Reactive]
        public string CurrentPassword { get; set; } = string.Empty;
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

        public ReactiveCommand<Unit, Unit>? ChangePasswordCommand { get; private set; }

        public ChangeAdminPasswordViewModel(IAdminAccessService? adminAccessService = null)
        {
            this.adminAccessService = adminAccessService ?? resolver.GetRequiredService<IAdminAccessService>();
        }

        protected override void SetupCommands()
        {
            base.SetupCommands();

            var isValid = this.IsValid();

            ChangePasswordCommand = ReactiveCommand.Create(ChangePassword, isValid);
        }

        private void ChangePassword()
        {
            bool isCurrentPassValid = adminAccessService.IsPasswordValid(CurrentPassword);
            isCurrentPasswordValid.OnNext(isCurrentPassValid);
            if (isCurrentPassValid)
            {
                adminAccessService.ChangeAdminPassword(NewPassword);
                DialogHost.CloseDialogCommand.Execute(true, null);
            }
        }

        protected override void SetupValidation(CompositeDisposable d)
        {
            base.SetupValidation(d);

            IObservable<bool> passwordsObservable =
                this.WhenAnyValue(
                    x => x.NewPassword,
                    x => x.PasswordConfirm,
                    (password, confirmation) => password == confirmation);

            IObservable<bool> newOldPasswordsObsevable =
                this.WhenAnyValue(
                    x => x.CurrentPassword,
                    x => x.NewPassword,
                    (oldPass, newPass) => oldPass != newPass);

            this.ValidationRule(
                vm => vm.CurrentPassword,
                password => !string.IsNullOrWhiteSpace(password),
                "Необходимо указать текущий пароль.");

            this.ValidationRule(
                vm => vm.NewPassword,
                password => !string.IsNullOrWhiteSpace(password),
                "Необходимо указать новый пароль.");

            this.ValidationRule(
                vm => vm.PasswordConfirm,
                password => !string.IsNullOrWhiteSpace(password),
                "Необходимо подтвердить пароль.");

            this.ValidationRule(
                vm => vm.PasswordConfirm,
                passwordsObservable,
                "Пароли должны совпадать.");

            this.ValidationRule(
                vm => vm.NewPassword,
                newOldPasswordsObsevable,
                "Новый пароль совпадает со старым");

            this.ValidationRule(
                vm => vm.CurrentPassword,
                isCurrentPasswordValid,
                "Неверный пароль"
                );
        }

        protected override void SetupSubscriptions(CompositeDisposable d)
        {
            base.SetupSubscriptions(d);

            this.WhenAnyValue(x => x.CurrentPassword)
                .Subscribe(passw => { isCurrentPasswordValid.OnNext(true); }).DisposeWith(d);
        }
    }
}
