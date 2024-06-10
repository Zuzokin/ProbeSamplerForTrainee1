using DynamicData;
using DynamicData.Binding;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows;

namespace ProbeSampler.Presentation
{
    public class ConfigManagerItem : AbstractNotifyPropertyChanged
    {
        public bool IsSelected { get; set; }

        public Guid Id { get; set; }

        public string? Name { get; set; }
    }

    public class ConfigManagerViewModel : RoutableViewModelBase, INestedMessageSender
    {
        private readonly IConnectionConfigurationManager manager;

        private readonly ICameraDisplayService cameraDisplayService;

        private readonly IApplicationSettingsManager applicationSettingsManager;
        private readonly IAdminAccessService adminAccessService;
        public readonly SourceCache<ConfigManagerItem, Guid> Connections = new(t => t.Id);

        [Reactive] public ISBMessageSender? MessageSender { get; set; }

        public ReactiveCommand<Guid, Unit>? EditConfigCommand { get; set; }

        public ReactiveCommand<Guid, Unit>? OpenInNewWindowCommand { get; set; }

        public ReactiveCommand<Guid, Unit>? OpenInNewTabCommand { get; set; }

        public ReactiveCommand<Guid, Unit>? RemoveConfigCommand { get; set; }

        public ReactiveCommand<Unit, Unit>? CreateConfigCommand { get; set; }

        public ReactiveCommand<Unit, Unit>? ChangePasswordCommand { get; set; }

        public ReactiveCommand<Unit, Unit>? LogInCommand { get; set; }

        public ReactiveCommand<Unit, Unit>? LogOutCommand { get; set; }

        [Reactive] public ChangePasswordMenuStatus ChangingPasswordStatus { get; set; } = ChangePasswordMenuStatus.Closed;

        [Reactive] public AuthorizationStatus AuthPasswordStatus { get; set; } = AuthorizationStatus.Closed;

        [Reactive] public string CurrentPassword { get; set; }

        [Reactive] public string NewPassword { get; set; }

        [Reactive] public string ConfirmPassword { get; set; }

        public ConfigManagerViewModel(IScreen? hostScreen = null)
            : base(hostScreen)
        {
            manager = resolver.GetRequiredService<IConnectionConfigurationManager>();
            cameraDisplayService = resolver.GetRequiredService<ICameraDisplayService>();
            applicationSettingsManager = resolver.GetRequiredService<IApplicationSettingsManager>();
            adminAccessService = resolver.GetRequiredService<IAdminAccessService>();
        }

        protected override void SetupStart()
        {
            base.SetupStart();
        }

        protected override void SetupSubscriptions(CompositeDisposable d)
        {
            base.SetupSubscriptions(d);

            Observable.Return("Менеджер подключений")
                .ToPropertyEx(this, x => x.ViewName)
                .DisposeWith(d);

            manager?.SourceCacheConnectionConfigurations
                .Connect()
                .Filter(config => config.Name != null)
                .Transform(it => new ConfigManagerItem { Id = it.Id, Name = it.Name })
                .Subscribe(it => UpdateConnectionItems(it))
                .DisposeWith(d);
        }

        protected override void SetupCommands()
        {
            base.SetupCommands();

            OpenInNewTabCommand = ReactiveCommand.Create<Guid, Unit>(OpenInNewTab);
            OpenInNewWindowCommand = ReactiveCommand.Create<Guid, Unit>(OpenNewWindow);
            EditConfigCommand = ReactiveCommand.Create<Guid, Unit>(EditConfig);
            RemoveConfigCommand = ReactiveCommand.Create<Guid, Unit>(RemoveConfig);
            CreateConfigCommand = ReactiveCommand.Create(CreateConfig);
            ChangePasswordCommand = ReactiveCommand.Create(() => ChangePassword(CurrentPassword, NewPassword, ConfirmPassword));
            LogInCommand = ReactiveCommand.CreateFromTask(() => LogIn());
            LogOutCommand = ReactiveCommand.Create(LogOut);
        }

        private Unit OpenNewWindow(Guid id)
        {
            cameraDisplayService?.OpenInNewWindow(id);

            return Unit.Default;
        }

        private Unit OpenInNewTab(Guid id)
        {
            cameraDisplayService?.OpenInNewTab(id);

            return Unit.Default;
        }

        private Unit EditConfig(Guid id)
        {
            if (AuthPasswordStatus == AuthorizationStatus.Success)
            {
                var configEditor = new ConfigEditorViewModel(id);
                HostScreen.Router.Navigate.Execute(configEditor);
                return Unit.Default;
            }
            else
            {
                this.SendMessageToBus("Для редактирования необходимо авторизоваться");
            }

            return Unit.Default;
        }

        private Unit RemoveConfig(Guid id)
        {
            if (AuthPasswordStatus == AuthorizationStatus.Success)
            {
                var messageBoxResult = MessageBox.Show("Вы точно хотите удалить подключение?", "Удаление подключения", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    var result = manager?.Remove(id);
                }
            }
            else
            {
                this.SendMessageToBus("Для удаления необходимо авторизоваться");
            }

            return Unit.Default;
        }

        private void UpdateConnectionItems(IChangeSet<ConfigManagerItem, Guid> changes)
        {
            List<ConfigManagerItem> items = changes.Select(t => t.Current).ToList();
            if (changes.Adds > 0 || changes.Updates > 0)
            {
                Connections.AddOrUpdate(items);
            }
            else if (changes.Removes > 0)
            {
                Connections.Remove(items);
            }
        }

        private void CreateConfig()
        {
            if (AuthPasswordStatus == AuthorizationStatus.Success)
            {
                var configEditor = new ConfigEditorViewModel();
                HostScreen.Router.Navigate.Execute(configEditor);
            }
            else
            {
                this.SendMessageToBus("Для создания необходимо авторизоваться");
            }
        }

        private void ChangePassword(string password, string newPassword, string confirmNewPassword)
        {
            try
            {
                var settings = applicationSettingsManager.GetSettings();

                if (password != null && settings != null)
                {
                    if (settings.AdminPassword == null)
                    {
                        settings.AdminPassword = PasswordHasher.GenerateIdentityV3Hash(newPassword);
                        ChangingPasswordStatus = ChangePasswordMenuStatus.Success;
                        return;
                    }

                    if (!PasswordHasher.VerifyIdentityV3Hash(password, settings.AdminPassword))
                    {
                        ChangingPasswordStatus = ChangePasswordMenuStatus.Failure;
                        return;
                    }

                    if (!PasswordHasher.IsNewPasswordValid(newPassword, confirmNewPassword))
                    {
                        ChangingPasswordStatus = ChangePasswordMenuStatus.Failure;
                        return;
                    }

                    settings.AdminPassword = PasswordHasher.GenerateIdentityV3Hash(newPassword);
                    applicationSettingsManager.UpdateSettings(settings, save: true);
                    ChangingPasswordStatus = ChangePasswordMenuStatus.Success;
                    this.Log().Info($"Password has been changed");
                    this.SendMessageToBus("Пароль изменен");
                }
            }
            catch
            {
                this.Log().Info($"failed attempt while change password");
                this.SendMessageToBus("Ошибка при смене пароля");
                ChangingPasswordStatus = ChangePasswordMenuStatus.Failure;
            }
        }

        private async Task LogIn()
        {
            bool isPassEntered = false;
            if (AuthPasswordStatus != AuthorizationStatus.Success)
            {
                isPassEntered = await ShowPasswordCheck();
            }

            AuthPasswordStatus = isPassEntered ? AuthorizationStatus.Success : AuthorizationStatus.WrongPassword;
        }

        private void LogOut()
        {
            AuthPasswordStatus = AuthorizationStatus.WaitForPassword;
            this.Log().Info($"logout");
            this.SendMessageToBus("Вы больше не находитесь в режиме дополнительного доступа. Ваши права доступа восстановлены к исходному уровню.");
        }

        private async Task<bool> ShowPasswordCheck()
        {
            var view = resolver.GetRequiredService<IViewFor<AdminPasswordRequestViewModel>>();
            AdminPasswordRequestViewModel viewModel = resolver.GetRequiredService<AdminPasswordRequestViewModel>();
            view.ViewModel = viewModel;
            var result = await DialogHost.Show(view, "RootDialog");
            return result is bool bResult && bResult;
        }
    }
}
