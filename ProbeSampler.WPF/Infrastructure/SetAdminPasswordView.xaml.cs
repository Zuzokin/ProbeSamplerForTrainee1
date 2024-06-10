namespace ProbeSampler.WPF
{
    public partial class SetAdminPasswordView
    {
        public SetAdminPasswordView()
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
                this.BindCommand(ViewModel, vm => vm.SetPasswordCommand, v => v.btnDialogAccept).DisposeWith(d);
            });
        }
    }
}
