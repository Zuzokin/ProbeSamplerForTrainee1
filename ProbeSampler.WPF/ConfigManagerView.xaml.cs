using DynamicData;
using System.Reactive.Linq;
using System.Windows;

namespace ProbeSampler.WPF
{
    public partial class ConfigManagerView
    {
        private ReadOnlyObservableCollection<ManagerItem>? managerItems;

        public ConfigManagerView()
        {
            InitializeComponent();
            SetupBinding();
        }

        private void SetupBinding()
        {
            this.WhenActivated(d =>
            {
                this.ViewModel?.Connections
                    .Connect()
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Transform(it => new ManagerItem { IsSelected = it.IsSelected, Id = it.Id, Name = it.Name })
                    .Bind(out managerItems)
                    .Subscribe()
                    .DisposeWith(d);
                this.WhenAnyValue(x => x.managerItems)
                    .BindTo(this, x => x.ConfigList.ItemsSource)
                    .DisposeWith(d);

                this.BindCommand(ViewModel, vm => vm.CreateConfigCommand, v => v.btnAddNewConConf).DisposeWith(d);
                // this.BindCommand(ViewModel, vm => vm.ChangePasswordCommand, v => v.btnChangePassword).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.LogInCommand, v => v.btnLogIn).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.LogOutCommand, v => v.btnLogOut).DisposeWith(d);

                this.WhenAnyValue(
                    x => x.ViewModel.ChangingPasswordStatus,
                    x => x.ViewModel.AuthPasswordStatus,
                    (changingPasswordStatus, authPasswordStatus) =>
                    {
                        //var changePasswordFailureVisible = (changingPasswordStatus == ChangePasswordMenuStatus.Failure) ? Visibility.Visible : Visibility.Collapsed;
                        var changePasswordSuccessVisible = (changingPasswordStatus == ChangePasswordMenuStatus.Success) ? Visibility.Visible : Visibility.Collapsed;
                        var authPasswordSuccessVisible = (authPasswordStatus == AuthorizationStatus.Success) ? Visibility.Visible : Visibility.Collapsed;
                        var authPasswordFailureVisible = (authPasswordStatus == AuthorizationStatus.WrongPassword) ? Visibility.Visible : Visibility.Collapsed;
                        var btnLogOutVisible = (authPasswordStatus == AuthorizationStatus.Success) ? Visibility.Visible : Visibility.Collapsed;
                        var btnLogInVisible = (authPasswordStatus != AuthorizationStatus.Success) ? Visibility.Visible : Visibility.Collapsed;

                        return (/*changePasswordFailureVisible,*/ changePasswordSuccessVisible, authPasswordSuccessVisible, authPasswordFailureVisible, btnLogOutVisible, btnLogInVisible);
                    })
                    .Subscribe(states =>
                    {
                        // ChangePasswordFailure.Visibility = states.changePasswordFailureVisible;
                        // ChangePasswordSucces.Visibility = states.changePasswordSuccessVisible;
                        AuthPasswordSucces.Visibility = states.authPasswordSuccessVisible;
                        //AuthPasswordFailure.Visibility = states.authPasswordFailureVisible;
                        btnLogOut.Visibility = states.btnLogOutVisible;
                        btnLogIn.Visibility = states.btnLogInVisible;
                    });

                this.WhenAnyValue(x => x.ViewModel)
                    .Subscribe(x =>
                    {
                        this.WhenAnyValue(x => x.ViewModel).BindTo(this, x => x.DataContext);
                    })
                    .DisposeWith(d);

                // this.WhenAnyValue(x => x.tabControl.SelectedIndex)
                //    .Subscribe(index =>
                //    {
                //        strPassword.Password = string.Empty;
                //        strCurrentPassword.Password = string.Empty;
                //        strNewPassword.Password = string.Empty;
                //        strConfrimPassword.Password = string.Empty;
                //    })
                //    .DisposeWith(d);
            });
        }

        class ManagerItem
        {
            public bool IsSelected { get; set; }

            public Guid Id { get; set; }

            public string? Name { get; set; }
        }
    }
}
