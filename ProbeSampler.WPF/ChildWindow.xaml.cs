using System.Reactive.Linq;

namespace ProbeSampler.WPF
{
    public partial class ChildWindow : IIdentifierProvider
    {
        private readonly IReadonlyDependencyResolver resolver = Locator.Current;
        private readonly string identifier = Guid.NewGuid().ToString();

        public string Identifier => identifier;

        public ChildWindow()
        {
            InitializeComponent();
            ViewModel = new ChildWindowViewModel(Identifier);

            this.WhenActivated(d =>
            {
                this.OneWayBind(ViewModel, vm => vm.Id, v => v.DialogHost.Identifier).DisposeWith(d);
                this.OneWayBind(ViewModel, vm => vm.Tabs, v => v.TabablzControl.ItemsSource).DisposeWith(d);

                MessageBus.Current.Listen<SBMessage>(Identifier)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(msg =>
                    {
                        ChildBotomSnackbar.MessageQueue?.Enqueue(msg, ChildBotomSnackbar);
                    })
                    .DisposeWith(d);
            });
        }
    }
}
