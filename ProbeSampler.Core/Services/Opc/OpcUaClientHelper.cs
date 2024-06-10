using DynamicData;
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

        /// <summary>
        /// Создание сертификата(не используются сертификаты при подключении)
        /// </summary>
        private void CreateCertificate()
        {
            Client = new OpcUaClient();
            Client.AppConfig.ApplicationName = "OPC UA Probe Sampler Client";
            Client.AppConfig.ApplicationUri = Utils.Format(@"urn:{0}:OPCClient", System.Net.Dns.GetHostName());
        }

        /// <summary>
        /// Подключение к OPC серверу и подписка на значения
        /// </summary>
        /// <param name="endpointURL"></param>
        /// <returns></returns>
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
                // todo tmp do not work, need to implement in opc server
                Client?.AddSubscription("Failure", "ns=1;s=SIEMENSPLC.siemensplc.Failure", SubCallback);
#elif (RELEASE)
                // todo tmp do not work, need to implement in opc server
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

        /// <summary>
        /// отключиться от OPC сервера
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// записать значение в ноду
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nodeIdStr">тэг узла</param>
        /// <param name="value">значение</param>
        /// <returns></returns>
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

        /// <summary>
        /// считывание значения
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nodeIdStr">тэг узла</param>
        /// <returns></returns>
        public async Task<T?> ReadValue<T>(string nodeIdStr)
        {
            // if (Client is null || (Client.State != OpcClientState.Connected && Client.State != OpcClientState.Reconnected))
            //    throw new OpcException($"There is no connection to the opc server, reading from node {nodeIdStr} cannot be performed");            
            return await Client?.ReadNodeAsync<T>(nodeIdStr);
        }

        /// <summary>
        /// считать все значения из OPC сервера
        /// </summary>
        /// <param name="numberOfPos">количество выбраных клеток</param>
        /// <returns>строку в которой содержатся значения OPC сервера</returns>
        public async Task<string> ReadValues(int numberOfPos)
        {
            var resultStringBuilder = new StringBuilder();
            var posStr = new List<string>();
            var otherStr = new List<string>();

            for (int i = 1; i <= numberOfPos; i++)
            {
                posStr.Add($"ns=1;s=SIEMENSPLC.siemensplc.pos mast {i}");
                posStr.Add($"ns=1;s=SIEMENSPLC.siemensplc.pos kopf {i}");
            }

            otherStr.Add("ns=1;s=SIEMENSPLC.siemensplc.anzahl_positiionen");
            otherStr.Add("ns=1;s=SIEMENSPLC.siemensplc.status_sps");
            otherStr.Add("ns=1;s=SIEMENSPLC.siemensplc.aktionen");

            // Запускаем чтение значений параллельно
            var posValuesTask = Client?.ReadNodesAsync<ushort>(posStr.ToArray());
            var otherValuesTask = Client?.ReadNodesAsync<int>(otherStr.ToArray());

            await Task.WhenAll(posValuesTask, otherValuesTask);

            // Получаем результаты чтения
            var posValues = await posValuesTask;
            var otherValues = await otherValuesTask;

            // Формируем строку с результатами
            for (int i = 0; i < posStr.Count; i += 2)
            {
                resultStringBuilder.AppendLine($"{posStr[i]} -- {posValues[i]}; {posStr[i + 1]} -- {posValues[i + 1]}");
            }

            for (int i = 0; i < otherStr.Count; i++)
            {
                resultStringBuilder.AppendLine($"{otherStr[i]} -- {otherValues[i]}");
            }

            return resultStringBuilder.ToString();
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

        // todo change hardcode maxdots or make const, better make maxdots in config
        public async Task ResetValues(byte MaxDots = 14)
        {
            var tasks = new List<Task>();

            for (int i = 1; i <= MaxDots; i++)
            {
                tasks.Add(WriteValue($"ns=1;s=SIEMENSPLC.siemensplc.pos mast {i}", 0));
                tasks.Add(WriteValue($"ns=1;s=SIEMENSPLC.siemensplc.pos kopf {i}", 0));
            }

            tasks.Add(WriteValue($"ns=1;s=SIEMENSPLC.siemensplc.anzahl_positiionen", 0));
            tasks.Add(WriteValue($"ns=1;s=SIEMENSPLC.siemensplc.status_sps", 255));
            tasks.Add(WriteValue($"ns=1;s=SIEMENSPLC.siemensplc.aktionen", 0));

            await Task.WhenAll(tasks);
        }

        // todo delete this useless shit
        public async Task<string> ReadAllValues(byte MaxDots = 14)
        {
            var statusMessage = $"Значения пробоотборника: {Environment.NewLine}";
            var opcServerValues = new List<string>();
            var tasks = new List<Task<string>>();

            for (int i = 1; i <= MaxDots; i++)
            {
                int index = i; // Захват переменной для использования в лямбда-выражении
                tasks.Add(ReadValue<int>($"ns=1;s=SIEMENSPLC.siemensplc.pos mast {index}")
                    .ContinueWith(t => $"pos mast {index} -- {t.Result}"));
                tasks.Add(ReadValue<int>($"ns=1;s=SIEMENSPLC.siemensplc.pos kopf {index}")
                    .ContinueWith(t => $"pos kopf {index} -- {t.Result}"));
            }

            tasks.Add(ReadValue<int>($"ns=1;s=SIEMENSPLC.siemensplc.anzahl_positiionen")
                .ContinueWith(t => $"anzahl_positiionen -- {t.Result}"));
            tasks.Add(ReadValue<int>($"ns=1;s=SIEMENSPLC.siemensplc.status_sps")
                .ContinueWith(t => $"status_sps -- {t.Result}"));
            tasks.Add(ReadValue<int>($"ns=1;s=SIEMENSPLC.siemensplc.aktionen")
                .ContinueWith(t => $"aktionen -- {t.Result}"));

            var results = await Task.WhenAll(tasks);

            statusMessage += string.Join(Environment.NewLine, results);

            return statusMessage;
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

        /// <summary>
        /// колбек на подписки
        /// </summary>
        /// <param name="key"></param>
        /// <param name="monitoredItem"></param>
        /// <param name="args"></param>
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

