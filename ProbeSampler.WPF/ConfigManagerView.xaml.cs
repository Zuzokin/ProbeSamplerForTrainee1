using System.Reactive.Linq;
using System.Windows;
using DynamicData;

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
