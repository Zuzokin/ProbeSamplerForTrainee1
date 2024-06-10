using System.Reactive.Linq;

namespace ProbeSampler.WPF
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainViewModel();

            this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel, x => x.CurrentViewName, v => v.ViewName.Text).DisposeWith(d);
                this.OneWayBind(ViewModel, x => x.Router, x => x.RootHost.Router).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.NavigateBackCommand, v => v.btnBack).DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.OpenSettingsCommand, v => v.btnAppSettings).DisposeWith(d);

                RootHost.Router.CurrentViewModel
                    .Subscribe(viewModel =>
                    {
                        if (viewModel is IActivatableViewModel activatableViewModel)
                        {
                            Disposable.Create(() => activatableViewModel.Activator.Deactivate())
                                .DisposeWith(d);
                        }
                    })
                    .DisposeWith(d);

                MessageBus.Current.Listen<SBMessage>("MainApplicationWindow")
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(msg =>
                    {
                        MainBotomSnackbar.MessageQueue?.Enqueue(msg, MainBotomSnackbar);
                    })
                    .DisposeWith(d);
            });
        }
    }
}
