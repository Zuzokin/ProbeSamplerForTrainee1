namespace ProbeSampler.Core.Services.Contract
{
    /// <summary>
    /// Менеджер, отвечающий за сохранение и загрузку хранимых сущностей.
    /// </summary>
    public interface IStorageService
    {
        /// <summary>
        /// Загрузить все объекты по пути.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pathToFolder"></param>
        /// <returns></returns>
        IEnumerable<T> Load<T>(string pathToFolder)
            where T : ISavableEntity;
        /// <summary>
        /// Загрузить все объекты по пути.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pathToFolder"></param>
        /// <returns></returns>
        Task<IEnumerable<T>> LoadAsync<T>(string pathToFolder)
            where T : ISavableEntity;
        /// <summary>
        /// Загрузить объект по имени.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nameToLoad"></param>
        /// <param name="pathToFolder"></param>
        /// <returns></returns>
        T? Load<T>(string nameToLoad, string pathToFolder)
            where T : ISavableEntity;
        /// <summary>
        /// Загрузить объект по имени.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nameToLoad"></param>
        /// <param name="pathToFolder"></param>
        /// <returns></returns>
        Task<T?> LoadAsync<T>(string nameToLoad, string pathToFolder)
            where T : ISavableEntity;
        /// <summary>
        /// Сохранить объект.
        /// </summary>
        /// <param name="objectToSave"></param>
        /// <param name="pathToFolder"></param>
        void Save(ISavableEntity objectToSave, string pathToFolder);
        /// <summary>
        /// Сохранить объект.
        /// </summary>
        /// <param name="objectToSave"></param>
        /// <param name="pathToFolder"></param>
        /// <returns></returns>
        Task SaveAsync(ISavableEntity objectToSave, string pathToFolder);
        /// <summary>
        /// Удалить объект.
        /// </summary>
        /// <param name="nameToRemove"></param>
        /// <param name="pathToFolder"></param>
        void Remove(string nameToRemove, string pathToFolder);
        /// <summary>
        /// Удалить объект.
        /// </summary>
        /// <param name="nameToRemove"></param>
        /// <param name="pathToFolder"></param>
        /// <returns></returns>
        Task RemoveAsync(string nameToRemove, string pathToFolder);
    }
}
