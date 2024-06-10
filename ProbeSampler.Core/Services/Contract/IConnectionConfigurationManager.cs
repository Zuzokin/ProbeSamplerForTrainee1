using DynamicData;

namespace ProbeSampler.Core.Services.Contract
{
    /// <summary>
    /// Менеджер подключений.
    /// </summary>
    public interface IConnectionConfigurationManager
    {
        /// <summary>
        /// Список подключений.
        /// </summary>
        SourceCache<ConnectionConfiguration, Guid> SourceCacheConnectionConfigurations { set; get; }
        /// <summary>
        /// Добавить подключение.
        /// </summary>
        /// <param name="connectionConfiguration"></param>
        /// <returns></returns>
        bool Add(ConnectionConfiguration connectionConfiguration);
        /// <summary>
        /// Добавить подключение.
        /// </summary>
        /// <param name="connectionConfiguration"></param>
        /// <returns></returns>
        Task<bool> AddAsync(ConnectionConfiguration connectionConfiguration);
        /// <summary>
        /// Удалить подключение.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        bool Remove(Guid id);
        /// <summary>
        /// Удалить подключение.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        bool Remove(string name);
        /// <summary>
        /// Удалить подключение.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<bool> RemoveAsync(Guid id);
        /// <summary>
        /// Удалить подключение.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<bool> RemoveAsync(string name);
        /// <summary>
        /// Проверка на наличие подключения с таким Id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        bool IsExistById(Guid id);
        /// <summary>
        /// Проверка на наличие подключения с таким названием.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        bool IsExistByName(string name);
        /// <summary>
        /// Получить подключения.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        ConnectionConfiguration? Get(Guid id);
        /// <summary>
        /// Получить подключения.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        ConnectionConfiguration? Get(string name);
    }
}
