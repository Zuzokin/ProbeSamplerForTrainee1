// using System.Text;
// using Opc.Ua;
// using Opc.UaFx;
// using Opc.UaFx.Client;
// using ProbeSampler.Core.Services.Contract;

// namespace ProbeSampler.Core.Services.Opc
// { для этой библиотеки нужна лицензия
//    public class OpcUaClient : IOpcUaClient
//    {
//        private SamplerConnection? connection;

// public BehaviorSubject<OPCUAConnectionStatus> ConnectionStatus { get; } = new(OPCUAConnectionStatus.None);

// public bool HaveAppCertificate { get; private set; }

// public OpcClient? Client;

// private OpcApplicationConfiguration? Configuration;

// //private CancellationTokenSource? internalCts;

// //public Session? Session;


// private void CreateCertificate()
//        {
//            Client = new OpcClient();

// Configuration = new OpcApplicationConfiguration(OpcApplicationType.Client)
//            {
//                ApplicationName = "OPC UA Probe Sampler Client",
//                ApplicationUri = Utils.Format(@"urn:{0}:OPCClient", System.Net.Dns.GetHostName())
//            };

// var securityConfiguration = Client.Configuration.SecurityConfiguration;
//            securityConfiguration.ApplicationCertificate.StorePath
//                    = Path.Combine(Environment.CurrentDirectory, "App Certificates");
//            securityConfiguration.RejectedCertificateStore.StorePath
//                    = Path.Combine(Environment.CurrentDirectory, "Rejected Certificates");
//            securityConfiguration.TrustedIssuerCertificates.StorePath
//                    = Path.Combine(Environment.CurrentDirectory, "Trusted Issuer Certificates");
//            securityConfiguration.TrustedIssuerCertificates.StorePath
//                    = Path.Combine(Environment.CurrentDirectory, "Trusted Peer Certificates");

// Client.Configuration = Configuration;
//        }

// public async Task Connect(string? endpointURL = default)
//        {
//            try
//            {
//                ConnectionStatus.OnNext(OPCUAConnectionStatus.Connecting);
//                var endpointURI = new Uri(connection?.URL ?? endpointURL ?? "");

// OpcClientIdentity? userIdentity = null;
//                if (connection?.UserName?.Length != 0 || connection?.Password?.Length != 0)
//                    userIdentity = new OpcClientIdentity(connection?.UserName, connection?.Password);

// if (Client != null)
//                {
//                    Client.Security.UserIdentity = userIdentity;
//                    Client.ServerAddress = endpointURI;
//                    Client.SessionName = "Session Name";
//                    Client.SessionTimeout = 60000;

// Client.Connect();
//                }

// ConnectionStatus.OnNext(OPCUAConnectionStatus.Connected);
//            }
//            catch (OpcException)
//            {
//                ConnectionStatus.OnNext(OPCUAConnectionStatus.Error);
//                throw;
//            }
//        }

// public async Task Disconnect()
//        {
//            try
//            {
//                await ResetValues();
//                Client?.Disconnect();
//                ConnectionStatus.OnNext(OPCUAConnectionStatus.Disconnected);
//            }
//            catch (OpcException)
//            {
//                ConnectionStatus.OnNext(OPCUAConnectionStatus.Error);
//                throw;
//            }
//        }

// public OpcStatus WriteValue<T>(string nodeIdStr, T value)
//        {
//            //if (Client is null || (Client.State != OpcClientState.Connected && Client.State != OpcClientState.Reconnected))
//            //    throw new OpcException($"There is no connection to the opc server, writing to node {nodeIdStr} cannot be performed");

// return Client.WriteNode(nodeIdStr, value);
//        }

// public T? ReadValue<T>(string nodeIdStr)
//        {
//            //if (Client is null || (Client.State != OpcClientState.Connected && Client.State != OpcClientState.Reconnected))
//            //    throw new OpcException($"There is no connection to the opc server, reading from node {nodeIdStr} cannot be performed");

// OpcValue nodeValue = Client.ReadNode(nodeIdStr);

// if (nodeValue.Status.IsGood)
//                return (T)Convert.ChangeType(nodeValue.Value, typeof(T));
//            else
//             throw new OpcException($"Читаемый тэг {nodeIdStr} недоступен. {nodeValue.Status}");
//        }


// public string GetAllNodesTree()
//        {
//            if (Client is null || (Client.State != OpcClientState.Connected && Client.State != OpcClientState.Reconnected))
//                throw new OpcException($"There is no connection to the opc server, browsing nodes cannot be performed");

// var node = Client.BrowseNode(OpcObjectTypes.ObjectsFolder);
//            return Browse(node, new StringBuilder()).ToString();
//        }

// private static StringBuilder Browse(OpcNodeInfo node, StringBuilder result, int level = 0)
//        {
//            var displayName = node.Attribute(OpcAttribute.DisplayName);

// result.AppendLine($"{new string(' ', level * 4)}{node.NodeId.ToString(OpcNodeIdFormat.Foundation)} ({displayName.Value})");

// // Browse the children of the node and continue browsing in preorder.
//            foreach (var childNode in node.Children())
//                Browse(childNode, result, level + 1);

// return result;
//        }

// private async Task ResetValues(byte MaxDots = 14)
//        {
//            for (int i = 1; i <= MaxDots; i++)
//            {
//                WriteValue($"ns=1;s=SIEMENSPLC.siemensplc.pos mast {i}", 0);
//                WriteValue($"ns=1;s=SIEMENSPLC.siemensplc.pos kopf {i}", 0);
//            }
//            WriteValue($"ns=1;s=SIEMENSPLC.siemensplc.anzahl_positiionen", 0);
//            WriteValue($"ns=1;s=SIEMENSPLC.siemensplc.status_sps", 255);
//        }

// private void SetState()
//        {
//            switch (Client?.State)
//            {
//                case OpcClientState.Connected:
//                    ConnectionStatus.OnNext(OPCUAConnectionStatus.Connected);
//                    break;
//                case OpcClientState.Connecting:
//                    ConnectionStatus.OnNext(OPCUAConnectionStatus.Connecting);
//                    break;

// case OpcClientState.Disconnected:
//                case OpcClientState.Disconnecting:
//                    ConnectionStatus.OnNext(OPCUAConnectionStatus.Disconnected);
//                    break;
//                case OpcClientState.Created:
//                    ConnectionStatus.OnNext(OPCUAConnectionStatus.Created);
//                    break;

// case OpcClientState.Reconnecting:
//                    ConnectionStatus.OnNext(OPCUAConnectionStatus.Reconnecting);
//                    break;
//                case OpcClientState.Reconnected:
//                    ConnectionStatus.OnNext(OPCUAConnectionStatus.Reconnected);
//                    break;

// default:
//                    ConnectionStatus.OnNext(OPCUAConnectionStatus.None);
//                    break;
//            }
//        }

// public bool IsServerAvaible()
//        {
//            throw new NotImplementedException();
//        }

// public async Task Initialize(SamplerConnection connection)
//        {
//            this.connection = connection;
//            CreateCertificate();
//        }

// public void Dispose()
//        {
//            if (ConnectionStatus.Value == OPCUAConnectionStatus.Connected)
//                Disconnect();
//        }
//    }
// }
