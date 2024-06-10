namespace ProbeSampler.WPF
{
    public partial class SamplerConnectionEditorView
    {
        public SamplerConnectionEditorView()
        {
            InitializeComponent();
            SetupBindings();
        }

        private void SetupBindings()
        {
            this.WhenActivated(d =>
            {
                this.Bind(ViewModel, vm => vm.URL, v => v.strOPCUAServerURI.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.UserName, v => v.strUserName.Text).DisposeWith(d);
                // TODO change strPassword.Password  System.Security.SecureString securePassword = strPassword.SecurePassword;
                // TODO change textbox to passwordbox in View

                // this.Bind(ViewModel, vm => vm.Password, v => v.strPassword.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.NodesTree, v => v.strNodesTree.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.LinearCalculationCoeffA, v => v.doubleLinearCalculationCoeffA.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.LinearCalculationCoeffB, v => v.doubleLinearCalculationCoeffB.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.RotationCalculationCoeffA, v => v.doubleRotationCalculationCoeffA.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.RotationCalculationCoeffB, v => v.doubleRotationCalculationCoeffB.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.RotationCalculationCoeffC, v => v.doubleRotationCalculationCoeffC.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.UnreachablePixels, v => v.intUnreachablePixels.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.BeakCoeff, v => v.intBeakCoeff.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.CellsNumberToSelect, v => v.intCellsToSelectQuantity.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.BigTruckWidth, v => v.intBigTruckWidth.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.CellsToSelectMultiplier, v => v.doubleCellsToSelectCoef.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.OffsetVelocity, v => v.doubleOffsetVelocity.Text).DisposeWith(d);
                this.Bind(ViewModel, vm => vm.RotationVelocity, v => v.doubleRotationVelocity.Text).DisposeWith(d);

                this.BindCommand(ViewModel, vm => vm.CheckOpcConnectionCommand, v => v.btnCheckOpcConnection).DisposeWith(d);

                this.WhenAnyValue(x => x.ViewModel)
                    .Subscribe(x =>
                    {
                        this.WhenAnyValue(x => x.ViewModel).BindTo(this, x => x.DataContext);
                    })
                    .DisposeWith(d);
            });
        }
    }
}
