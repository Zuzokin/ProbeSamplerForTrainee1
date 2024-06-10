using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Windows;
using DynamicData;
using DynamicData.Binding;
using Opc.Ua;
using ProbeSampler.Presentation.Helpers;

namespace ProbeSampler.Presentation
{
    public class ConfigManagerItem : AbstractNotifyPropertyChanged
    {
        public bool IsSelected { get; set; }
        public Guid Id { get; set; }
        public string? Name { get; set; }
    }

    public class ConfigManagerViewModel : RoutableViewModelBase
    {
        private readonly IConnectionConfigurationManager manager;

        private readonly ICameraDisplayService cameraDisplayService;

        private readonly IApplicationSettingsManager applicationSettingsManager;

        public readonly SourceCache<ConfigManagerItem, Guid> Connections = new(t => t.Id);

        public ReactiveCommand<Guid, Unit>? EditConfigCommand { get; set; }
        public ReactiveCommand<Guid, Unit>? OpenInNewWindowCommand { get; set; }
        public ReactiveCommand<Guid, Unit>? OpenInNewTabCommand { get; set; }
        public ReactiveCommand<Guid, Unit>? RemoveConfigCommand { get; set; }
        public ReactiveCommand<Unit, Unit>? CreateConfigCommand { get; set; }

        public ReactiveCommand<Unit, Unit>? ChangePasswordCommand { get; set; }
        public ReactiveCommand<Unit, Unit>? ShowChangePasswordMenuCommand { get; set; }
        [Reactive] public PasswordMenuStatus PasswordStatus { get; set; } = PasswordMenuStatus.Closed;
        [Reactive] public string CurrentPassword { get; set; }
        [Reactive] public string NewPassword { get; set; }
        [Reactive] public string ConfirmPassword { get; set; }


        public ConfigManagerViewModel(IScreen? hostScreen = null) : base(hostScreen)
        {
            manager = resolver.GetRequiredService<IConnectionConfigurationManager>();
            cameraDisplayService = resolver.GetRequiredService<ICameraDisplayService>();
            applicationSettingsManager = resolver.GetRequiredService<IApplicationSettingsManager>();
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
            ShowChangePasswordMenuCommand = ReactiveCommand.Create(ShowPasswordMenu);
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
            var configEditor = new ConfigEditorViewModel(id);
            HostScreen.Router.Navigate.Execute(configEditor);
            return Unit.Default;
        }

        private Unit RemoveConfig(Guid id)
        {
            var result = manager?.Remove(id);

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
            var configEditor = new ConfigEditorViewModel();
            HostScreen.Router.Navigate.Execute(configEditor);
        }

        //todo add ex handler
        private void ChangePassword(string Password, string NewPassword, string ConfirmNewPassword)
        {
            try
            {
                var settings = applicationSettingsManager.GetSettings();

                if (Password != null && settings != null)

                {
                    if (!PasswordHasher.VerifyIdentityV3Hash(Password, settings.AdminPassword))
                    {
                        PasswordStatus = PasswordMenuStatus.Failure;
                        return;
                    };
                    if (!PasswordHasher.isNewPasswordValid(NewPassword, ConfirmNewPassword))
                    {
                        PasswordStatus = PasswordMenuStatus.Failure;
                        return;
                    }
                    settings.AdminPassword = PasswordHasher.GenerateIdentityV3Hash(NewPassword);
                    applicationSettingsManager.UpdateSettings(settings, save: true);
                    PasswordStatus = PasswordMenuStatus.Success;
                }
            }
            catch
            {
                PasswordStatus = PasswordMenuStatus.Failure;
            }

        }

        private void ShowPasswordMenu()
        {
            PasswordStatus = (PasswordStatus == PasswordMenuStatus.Closed) ? PasswordMenuStatus.WaitForPassword : PasswordMenuStatus.Closed;
        }
    }
}
