using OpcUaHelper;

namespace ProbeSampler.Core.Services.Contract
{
    /// <summary>
    /// Клиент OPCUA, проще говоря связь этой программы с АСУТП частью.
    /// </summary>
    public interface IOpcUaClient : IAsyncDisposable
    {
        /// <summary>
        /// Статус подключения.
        /// </summary>
        BehaviorSubject<OPCUAConnectionStatus> ConnectionStatus { get; }

        BehaviorSubject<bool> FailureValue { get; }
        BehaviorSubject<bool> IsSampleAvailable { get; }
        BehaviorSubject<bool> GateFailureValue { get; }
        //BehaviorSubject<bool> IsSampleTaken { get; }

        BehaviorSubject<SpsStatus> SpsStatusValue { get; }

        public OpcUaClient? Client { get; }

        /// <summary>
        /// Наличие сертификата(не реализованно, необходимо подтверждение сертификата у сервера).
        /// </summary>
        bool HaveAppCertificate { get; }

        /// <summary>
        /// Инициализация клиента.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        void Initialize(SamplerConnection connection);

        /// <summary>
        /// Подключение к серверу.
        /// </summary>
        /// <param name="endpointURL">ссылка OpcUaсСервера.</param>
        /// <returns></returns>
        Task Connect(string? endpointURL = default);
        /// <summary>
        /// Отключение от сервера.
        /// </summary>
        /// <returns></returns>
        Task Disconnect();

        /// <summary>
        /// Записать значение в тэг Opc сервера.
        /// </summary>
        /// <typeparam name="T">Тип значения для записи.</typeparam>
        /// <param name="nodeIdStr">Uri тэга.</param>
        /// <param name="value">Значение, которое необходимо записать.</param>
        /// <returns>Успешно или нет запись в тэг.</returns>
        public Task<bool> WriteValue<T>(string nodeIdStr, T value);
        /// <summary>
        /// Получить значение с тэга Opc сервера.
        /// </summary>
        /// <typeparam name="T">Тип полученного значения.</typeparam>
        /// <param name="nodeIdStr">Uri тэга.</param>
        /// <returns>Полученное значение.</returns>
        public Task<T?> ReadValue<T>(string nodeIdStr);

        /// <summary>
        /// Получить список всех тэгов Opc сервера (Захардкожен "ns=1;s=SIEMENSPLC как корневой тэг).
        /// </summary>
        /// <returns>Список в виде строки всех тегов и их значений.</returns>
        public string GetAllNodesTree();

        public Task ResetValues(byte MaxDots);
        /// <summary>
        /// Возвращает значение true или false, в зависимости подключен ли клиент к opc серверу
        /// </summary>
        /// <returns></returns>
    }
}
