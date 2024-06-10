namespace ProbeSampler.WPF
{
    public partial class AdminPasswordRequestView
    {
        public AdminPasswordRequestView()
        {
            InitializeComponent();

            this.WhenActivated(d =>
            {
                this.WhenAnyValue(x => x.ViewModel)
                    .Subscribe(x =>
                    {
                        this.WhenAnyValue(x => x.ViewModel).BindTo(this, x => x.DataContext);
                    })
                    .DisposeWith(d);
                this.BindCommand(ViewModel, vm => vm.GetAdditionalPermissionsCommand, v => v.btnDialogAccept).DisposeWith(d);
            });
        }
    }
}
