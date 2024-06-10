namespace ProbeSampler.WPF
{
    public partial class ChangeAdminPasswordView
    {
        public ChangeAdminPasswordView()
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
                this.BindCommand(ViewModel, vm => vm.ChangePasswordCommand, v => v.btnDialogAccept).DisposeWith(d);
            });
        }
    }
}
