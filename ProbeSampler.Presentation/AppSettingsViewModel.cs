using MaterialDesignThemes.Wpf;
using System.Reactive;
using System.Threading.Tasks;

namespace ProbeSampler.Presentation
{
    public class AppSettingsViewModel : RoutableViewModelBase
    {
        private readonly IApplicationSettingsManager applicationSettingsManager;
        private readonly IAdminAccessService adminAccessService;

        [ObservableAsProperty]
        public bool IsPasswordSet { get; }

        public ReactiveCommand<Unit, Unit>? SetPasswordCommand { get; private set; }

        public ReactiveCommand<Unit, Unit>? ChangePasswordCommand { get; private set; }

        public AppSettingsViewModel()
        {
            applicationSettingsManager = resolver.GetRequiredService<IApplicationSettingsManager>();
            adminAccessService = resolver.GetRequiredService<IAdminAccessService>();
        }

        protected override void SetupStart()
        {
            base.SetupStart();

            if (!adminAccessService.IsPasswordSet)
            {
                SetPasswordCommand?.Execute().Subscribe();
            }
        }

        protected override void SetupSubscriptions(CompositeDisposable d)
        {
            base.SetupSubscriptions(d);

            Observable.Return("Настройки приложения")
                .ToPropertyEx(this, x => x.ViewName)
                .DisposeWith(d);

            adminAccessService.IsPasswordSetObservable
                .ToPropertyEx(this, vm => vm.IsPasswordSet)
                .DisposeWith(d);
        }

        protected override void SetupCommands()
        {
            base.SetupCommands();

            ChangePasswordCommand = ReactiveCommand.Create(ChangePassword);
            SetPasswordCommand = ReactiveCommand.Create(SetPassword);
        }

        private async void SetPassword()
        {
            await OpenPopupCore<SetAdminPasswordViewModel>();
        }

        private async void ChangePassword()
        {
            await OpenPopupCore<ChangeAdminPasswordViewModel>();
        }

        private async Task<bool> OpenPopupCore<T>(string hostIdentifier = "RootDialog")
            where T : ViewModelBase
        {
            var view = resolver.GetRequiredService<IViewFor<T>>();
            var viewModel = resolver.GetRequiredService<T>();
            view.ViewModel = viewModel;
            return await DialogHost.Show(view, hostIdentifier) is bool bResult && bResult;
        }
    }
}
