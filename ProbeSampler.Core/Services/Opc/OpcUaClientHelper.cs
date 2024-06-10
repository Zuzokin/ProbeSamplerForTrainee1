using Opc.Ua;
using Opc.Ua.Client;
using OpcUaHelper;
using System.Reactive.Linq;
using System.Text;

namespace ProbeSampler.Core.Services.Opc
{
    public class OpcUaClientHelper : IOpcUaClient
    {
        private SamplerConnection? connection;
        private bool disposed = false;
        public BehaviorSubject<OPCUAConnectionStatus> ConnectionStatus { get; } = new(OPCUAConnectionStatus.None);
        public BehaviorSubject<bool> FailureValue { get; } = new BehaviorSubject<bool>(false);
        public BehaviorSubject<bool> GateFailureValue { get; } = new BehaviorSubject<bool>(false);
        public BehaviorSubject<bool> IsSampleAvailable { get; } = new BehaviorSubject<bool>(true);
        public BehaviorSubject<SpsStatus> SpsStatusValue { get; } = new BehaviorSubject<SpsStatus>(SpsStatus.Waiting);

        public bool HaveAppCertificate { get; private set; }

        public OpcUaClient? Client { get; set; }


        ~OpcUaClientHelper()
        {
            CleanUpResources(false).AsTask().Wait();
        }

        private void CreateCertificate()
        {
            Client = new OpcUaClient();
            Client.AppConfig.ApplicationName = "OPC UA Probe Sampler Client";
            Client.AppConfig.ApplicationUri = Utils.Format(@"urn:{0}:OPCClient", System.Net.Dns.GetHostName());
        }

        public async Task Connect(string? endpointURL = default)
        {
            try
            {
                ConnectionStatus.OnNext(OPCUAConnectionStatus.Connecting);
                var endpointURI = new Uri(connection?.URL ?? endpointURL ?? "");

                // UserIdentity? userIdentity = new UserIdentity(new AnonymousIdentityToken());

                UserIdentity? userIdentity = null;
                if (connection?.UserName?.Length != 0 || connection?.Password?.Length != 0)
                {
                    userIdentity = new UserIdentity(connection?.UserName, connection?.Password);
                }

                if (Client != null)
                {
                    Client.UserIdentity = userIdentity;
                    await Client.ConnectServer(endpointURI.ToString());
                }                

                if (FailureValue.Value)
                    ConnectionStatus.OnNext(OPCUAConnectionStatus.ErrorNoConnectionToProbe);
                else
                    ConnectionStatus.OnNext(OPCUAConnectionStatus.Connected);       

                await WriteValue($"ns=1;s=SIEMENSPLC.siemensplc.status_sps", 255);
#if (DEBUG)
                Client?.AddSubscription("Failure", "ns=1;s=SIEMENSPLC.siemensplc.Failure", SubCallback);
#elif (RELEASE)
                Client?.AddSubscription("gate failure status", "ns=1;s=SIEMENSPLC.siemensplc.gate failure status", SubCallback);
                Client?.AddSubscription("Failure", "ns=1;s=SIEMENSPLC.siemensplc.Diagnosis.Failure", SubCallback);
#endif
                Client?.AddSubscription("status_sps", "ns=1;s=SIEMENSPLC.siemensplc.status_sps", SubCallback);
                Client?.AddSubscription("is sample taken", "ns=1;s=SIEMENSPLC.siemensplc.is sample taken", SubCallback);
                Client?.AddSubscription("is sample available", "ns=1;s=SIEMENSPLC.siemensplc.is sample available", SubCallback);
                
                //await WriteValue($"ns=1;s=SIEMENSPLC.siemensplc.status_sps", 255);
                //SpsStatusValue.OnNext(SpsStatus.Waiting);

            }
            catch (Exception)
            {
                ConnectionStatus.OnNext(OPCUAConnectionStatus.Error);
                throw;
            }
        }

        public async Task Disconnect()
        {
            try
            {
                await ResetValues();
                Client?.Disconnect();
                ConnectionStatus.OnNext(OPCUAConnectionStatus.Disconnected);                
            }
            catch (Exception)
            {
                ConnectionStatus.OnNext(OPCUAConnectionStatus.Error);
                throw;
            }
        }

        public async Task<bool> WriteValue<T>(string nodeIdStr, T value)
        {
            // if (Client is null || (Client.State != OpcClientState.Connected && Client.State != OpcClientState.Reconnected))
            //    throw new OpcException($"There is no connection to the opc server, writing to node {nodeIdStr} cannot be performed");

            if (Client == null)
            {
                return false;
            }

            return await Client.WriteNodeAsync(nodeIdStr, value);
        }

        public async Task<T?> ReadValue<T>(string nodeIdStr)
        {
            // if (Client is null || (Client.State != OpcClientState.Connected && Client.State != OpcClientState.Reconnected))
            //    throw new OpcException($"There is no connection to the opc server, reading from node {nodeIdStr} cannot be performed");

            return await Client?.ReadNodeAsync<T>(nodeIdStr);
        }

        public bool GetNodeStatus()
        {
            throw new NotImplementedException();
        }

        public string GetAllNodesTree()
        {
            // if (Client is null ||  Client.Session.Connected)
            //    throw new Exception($"There is no connection to the opc server, browsing nodes cannot be performed");
            string nodeId = "ns=1;s=SIEMENSPLC";
            return Browse(nodeId, new StringBuilder()).ToString();
        }

        private StringBuilder Browse(string nodeId, StringBuilder result, int level = 0)
        {
            level++;
            foreach (var childNode in Client?.BrowseNodeReference(nodeId))
            {
                string stepLevel = new string(' ', level * 4);
                string childNodeId = $"ns={childNode.NodeId.NamespaceIndex};{childNode.NodeId.Identifier}";

                var ValueAttributes = Client?.ReadNoteDataValueAttributes(childNodeId);
                OpcNodeAttribute[] nodeAttributes = Client.ReadNoteAttributes(childNodeId);

                //result.AppendLine($"{nodeAttributes}");

                result.AppendLine($"{stepLevel}{childNodeId} --- {ValueAttributes[12].Value} --- {ValueAttributes[0].StatusCode}");
                Browse($"{childNodeId}", result, level);
            }

            return result;
        }

        // todo change hardcode maxdots
        public async Task ResetValues(byte MaxDots = 14)
        {
            for (int i = 1; i <= MaxDots; i++)
            {
                await WriteValue($"ns=1;s=SIEMENSPLC.siemensplc.pos mast {i}", 0);
                await WriteValue($"ns=1;s=SIEMENSPLC.siemensplc.pos kopf {i}", 0);
            }

            await WriteValue($"ns=1;s=SIEMENSPLC.siemensplc.anzahl_positiionen", 0);
            await WriteValue($"ns=1;s=SIEMENSPLC.siemensplc.status_sps", 255);
        }

        public void Initialize(SamplerConnection connection)
        {
            this.connection = connection;
            CreateCertificate();
        }

        public async ValueTask DisposeAsync()
        {
            await CleanUpResources(true);
            disposed = true;
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask CleanUpResources(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    
                }

                if (ConnectionStatus.Value == OPCUAConnectionStatus.Connected)
                {
                    await Disconnect();
                }
                SpsStatusValue.Dispose();
                FailureValue.Dispose();
                GateFailureValue.Dispose();
                IsSampleAvailable.Dispose();
                ConnectionStatus.Dispose();
            }
        }

        private void SubCallback(string key, MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs args)
        {
            if (key == "Failure")
            {
                MonitoredItemNotification? notification = args.NotificationValue as MonitoredItemNotification;
                if (notification != null)
                {
                    FailureValue.OnNext((bool)notification.Value.Value);
                }
            }

            if (key == "status_sps")
            {
                MonitoredItemNotification? notification = args.NotificationValue as MonitoredItemNotification;
                if (notification != null)
                {
                    SpsStatusValue.OnNext((SpsStatus)notification.Value.Value);
                }
            }
            if (key == "is sample available")
            {
                MonitoredItemNotification? notification = args.NotificationValue as MonitoredItemNotification;
                if (notification != null)
                {
                    IsSampleAvailable.OnNext((bool)notification.Value.Value);
                }
            }            
            if (key == "gate failure status")
            {
                MonitoredItemNotification? notification = args.NotificationValue as MonitoredItemNotification;
                if (notification != null)
                {
                    // good - 0
                    // bad - 2147483648
                    // out of service - 2156724224 ( subcode - 9240576)
                    var test = notification.Value.StatusCode.ToString();
                    Console.WriteLine(test);
                    if (notification.Value.StatusCode == 2147483648 || notification.Value.StatusCode == 2156724224)
                    {
                        GateFailureValue.OnNext(true);
                    }
                    else
                    {
                        GateFailureValue.OnNext(false);
                    }
                }
            }            
        }
    }
}

