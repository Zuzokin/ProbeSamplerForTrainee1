using Opc.UaFx;

namespace ProbeSampler.Core.Services.Contract
{
    /// <summary>
    /// Клиент OPCUA, проще говоря связь этой программы с АСУТП частью
    /// </summary>
    public interface IOpcUaClient
    {
        /// <summary>
        /// Статус подключения
        /// </summary>
        BehaviorSubject<OPCUAConnectionStatus> ConnectionStatus { get; }
        /// <summary>
        /// Наличие сертификата
        /// </summary>
        bool HaveAppCertificate { get; }
        /// <summary>
        /// Инициализация клиента
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        Task Initialize(SamplerConnection connection);
        
        /// <summary>
        /// Подключение к серверу
        /// </summary>
        /// <param name="endpointURL">ссылка OpcUaсСервера</param>
        /// <returns></returns>
        Task Connect(string? endpointURL = default);
        /// <summary>
        /// Отключение от сервера
        /// </summary>
        /// <returns></returns>
        Task Disconnect();

        public OpcStatus WriteValue<T>(string nodeIdStr, T value);

        public T? ReadValue<T>(string nodeIdStr);

        public string GetAllNodesTree();
        void Dispose();
    }
}
