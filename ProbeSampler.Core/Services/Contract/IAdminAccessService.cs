namespace ProbeSampler.Core.Services.Contract
{
    /// <summary>
    /// Сервис управления доступом по паролю администратора.
    /// </summary>
    public interface IAdminAccessService
    {
        bool IsPasswordSet { get; }
        /// <summary>
        /// Пароль администратора установлен.
        /// </summary>
        IObservable<bool> IsPasswordSetObservable { get; }

        bool IsAccessGranted { get; }
        /// <summary>
        /// Подписка на изменение статуса доступа.
        /// </summary>
        IObservable<bool> IsAccessGrantedObservable { get; }

        /// <summary>
        /// Проверка пароля.
        /// </summary>
        /// <param name="password"></param>
        bool IsPasswordValid(string password);

        /// <summary>
        /// Сменить пароль администратора.
        /// </summary>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        bool ChangeAdminPassword(string newPassword);
    }
}
