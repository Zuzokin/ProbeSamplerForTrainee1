using MaterialDesignThemes.Wpf;
using ReactiveUI.Validation.Extensions;
using System.Reactive;
using System.Reactive.Subjects;

namespace ProbeSampler.Presentation
{
    public class AdminPasswordRequestViewModel : ViewModelBase
    {
        private readonly IAdminAccessService adminAccessService;
        private BehaviorSubject<bool> isPasswordValid = new BehaviorSubject<bool>(true);

        /// <summary>
        /// Пароль.
        /// </summary>
        [Reactive]
        public string Password { get; set; } = string.Empty;

        public ReactiveCommand<Unit, Unit>? GetAdditionalPermissionsCommand { get; private set; }

        public AdminPasswordRequestViewModel(IAdminAccessService? adminAccessService = null)
        {
            this.adminAccessService = adminAccessService ?? resolver.GetRequiredService<IAdminAccessService>();
        }

        protected override void SetupCommands()
        {
            base.SetupCommands();

            var isValid = this.IsValid();

            GetAdditionalPermissionsCommand = ReactiveCommand.Create(GetAdditionalPermissions, isValid);
        }

        private void GetAdditionalPermissions()
        {
            bool isPassValid = adminAccessService.IsPasswordValid(Password);
            isPasswordValid.OnNext(isPassValid);
            if (isPassValid)
            {
                // adminAccessService.ChangeAdminPassword(Password);
                DialogHost.CloseDialogCommand.Execute(true, null);
            }
        }

        protected override void SetupValidation(CompositeDisposable d)
        {
            base.SetupValidation(d);

            this.ValidationRule(
                vm => vm.Password,
                password => !string.IsNullOrWhiteSpace(password),
                "Необходимо указать текущий пароль.");

            this.ValidationRule(
                vm => vm.Password,
                isPasswordValid,
                "Неверный пароль");
        }

        protected override void SetupSubscriptions(CompositeDisposable d)
        {
            base.SetupSubscriptions(d);

            this.WhenAnyValue(x => x.Password)
                .Subscribe(pwd => { isPasswordValid.OnNext(true); }).DisposeWith(d);
        }
    }
}
