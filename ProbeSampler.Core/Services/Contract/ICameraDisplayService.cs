namespace ProbeSampler.Core.Services.Contract
{
    /// <summary>
    /// Менеджер окон подключений. Предоставляет методы для открытия подключения.
    /// </summary>
    public interface ICameraDisplayService
    {
        /// <summary>
        /// Открыть подключение в новом окне.
        /// </summary>
        /// <param name="id"></param>
        void OpenInNewWindow(Guid id);
        /// <summary>
        /// Открыть подключение в существующем окне, но в новой вкладке.
        /// </summary>
        /// <param name="id"></param>
        void OpenInNewTab(Guid id);
    }
}
