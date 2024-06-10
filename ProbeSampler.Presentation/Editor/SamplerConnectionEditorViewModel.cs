using System.Reactive;
using ProbeSampler.Core.Entities;
using System.Threading.Tasks;
using Opc.UaFx;

namespace ProbeSampler.Presentation
{
    public class SamplerConnectionEditorViewModel : ViewModelBase, ITabContent
    {
        private IOpcUaClient opcUaClient;
        [Reactive] public ISBMessageSender? MessageSender { get; set; }

        [Reactive] public ConnectionConfiguration ConnectionConfiguration { get; private set; }
        [Reactive] public string? URL { get; set; }
        [Reactive] public string? UserName { get; set; }
        [Reactive] public string? Password { get; set; }
        [Reactive] public string? NodesTree { get; set; }
        [Reactive] public double? LinearCalculationCoeffA { get; set; }
        [Reactive] public double? LinearCalculationCoeffB { get; set; }
        [Reactive] public double? RotationCalculationCoeffA { get; set; }
        [Reactive] public double? RotationCalculationCoeffB { get; set; }
        [Reactive] public double? RotationCalculationCoeffC { get; set; }
        [Reactive] public int? UnreachablePixels { get; set; }
        [Reactive] public int? BeakCoeff { get; set; }
        public ReactiveCommand<Unit, Unit>? CheckOpcConnectionCommand { get; set; }



        public SamplerConnectionEditorViewModel(ConnectionConfiguration connectionConfiguration, IOpcUaClient? opcUaClient = null)
        {
            ConnectionConfiguration = connectionConfiguration;
            this.opcUaClient = opcUaClient ?? resolver.GetRequiredService<IOpcUaClient>();
        }

        protected override void SetupSubscriptions(CompositeDisposable d)
        {
            base.SetupSubscriptions(d);

            this.WhenAnyValue(x => x.ConnectionConfiguration)
            .Where(x => x != null)
            .Subscribe(connection =>
            {
                if (connection?.SamplerConnection != null)
                {
                    URL = connection.SamplerConnection.URL;
                    UserName = connection.SamplerConnection.UserName;
                    Password = connection.SamplerConnection.Password;
                    LinearCalculationCoeffA = connection.SamplerConnection.LinearCalculationCoeffA;
                    LinearCalculationCoeffB = connection.SamplerConnection.LinearCalculationCoeffB;
                    RotationCalculationCoeffA = connection.SamplerConnection.RotationCalculationCoeffA;
                    RotationCalculationCoeffB = connection.SamplerConnection.RotationCalculationCoeffB;
                    RotationCalculationCoeffC = connection.SamplerConnection.RotationCalculationCoeffC;
                    BeakCoeff = connection.SamplerConnection.BeakCoeff;
                    UnreachablePixels = connection.SamplerConnection.UnreachablePixels;
                    
                    Task.Run(() => opcUaClient.Initialize(connection.SamplerConnection))
                    .DisposeWith(d);
                }

                if (connection?.SamplerConnection?.URL != null)
                    URL = connection.SamplerConnection.URL;

                this.WhenAnyValue(x => x.URL)
                    .Subscribe(viewURL =>
                    {
                        if (ConnectionConfiguration?.SamplerConnection != null)
                            ConnectionConfiguration.SamplerConnection.URL = viewURL;
                    })
                    .DisposeWith(d);

                this.WhenAnyValue(x => x.UserName)
                   .Subscribe(viewUserName =>
                   {
                       if (ConnectionConfiguration?.SamplerConnection != null)
                           ConnectionConfiguration.SamplerConnection.UserName = viewUserName;
                   })
                   .DisposeWith(d);

                this.WhenAnyValue(x => x.Password)
                    .Subscribe(viewPassword =>
                    {
                        if (ConnectionConfiguration?.SamplerConnection != null)
                            ConnectionConfiguration.SamplerConnection.Password = viewPassword;
                    })
                    .DisposeWith(d);
                this.WhenAnyValue(x => x.LinearCalculationCoeffA)
                    .Subscribe(viewLinearCalculationCoeffA =>
                    {
                        if (ConnectionConfiguration?.SamplerConnection != null)
                            ConnectionConfiguration.SamplerConnection.LinearCalculationCoeffA = viewLinearCalculationCoeffA;
                    })
                    .DisposeWith(d);
                this.WhenAnyValue(x => x.LinearCalculationCoeffB)
                    .Subscribe(viewLinearCalculationCoeffB =>
                    {
                        if (ConnectionConfiguration?.SamplerConnection != null)
                            ConnectionConfiguration.SamplerConnection.LinearCalculationCoeffB = viewLinearCalculationCoeffB;
                    })
                    .DisposeWith(d);
                this.WhenAnyValue(x => x.RotationCalculationCoeffA)
                    .Subscribe(viewRotationCalculationCoeffA =>
                    {
                        if (ConnectionConfiguration?.SamplerConnection != null)
                            ConnectionConfiguration.SamplerConnection.RotationCalculationCoeffA = viewRotationCalculationCoeffA;
                    })
                    .DisposeWith(d);                
                this.WhenAnyValue(x => x.RotationCalculationCoeffB)
                    .Subscribe(viewRotationCalculationCoeffB =>
                    {
                        if (ConnectionConfiguration?.SamplerConnection != null)
                            ConnectionConfiguration.SamplerConnection.RotationCalculationCoeffB = viewRotationCalculationCoeffB;
                    })
                    .DisposeWith(d);                
                this.WhenAnyValue(x => x.RotationCalculationCoeffC)
                    .Subscribe(viewRotationCalculationCoeffC =>
                    {
                        if (ConnectionConfiguration?.SamplerConnection != null)
                            ConnectionConfiguration.SamplerConnection.RotationCalculationCoeffC = viewRotationCalculationCoeffC;
                    })
                    .DisposeWith(d);                
                this.WhenAnyValue(x => x.UnreachablePixels) 
                    .Subscribe(viewUnreachablePixels =>
                    {
                        if (ConnectionConfiguration?.SamplerConnection != null)
                            ConnectionConfiguration.SamplerConnection.UnreachablePixels = viewUnreachablePixels;
                    })
                    .DisposeWith(d);                
                this.WhenAnyValue(x => x.BeakCoeff)
                    .Subscribe(viewBeakCoeff =>
                    {
                        if (ConnectionConfiguration?.SamplerConnection != null)
                            ConnectionConfiguration.SamplerConnection.BeakCoeff = viewBeakCoeff;
                    })
                    .DisposeWith(d);
            })
            .DisposeWith(d);

        }

        protected override void SetupCommands()
        {
            base.SetupCommands();

            CheckOpcConnectionCommand = ReactiveCommand.Create(CheckOpcConnect);
            //SelectImageFileCommand = ReactiveCommand.Create(SelectImageFile);
        }



        private async void CheckOpcConnect()
        {
            try
            {
                NodesTree = "";
                if (opcUaClient is null)
                    throw new OpcException("OpcUaClient is not created");

                await opcUaClient.Connect();

                if (opcUaClient.ReadValue<bool>("ns=1;s=SIEMENSPLC.siemensplc.Diagnosis.Failure"))
                    throw new OpcException("opcUaClient connected, but where is no connection to ProbeSampler.");

                NodesTree = opcUaClient.GetAllNodesTree();                
            }
            catch (Exception ex)
            {
                this.Log().Error($"Error when check connection to OpcServer: {ex}");
                this.SendMessageToBus($"Ошибка при подключении к серверу {ex}");
            }
            finally
            {
                opcUaClient?.Disconnect();
            }

        }
    }
}
