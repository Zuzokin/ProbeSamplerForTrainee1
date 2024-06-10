namespace ProbeSampler.Core.Entities
{
    /// <summary>
    /// Настройки приложения.
    /// </summary>
    public class ApplicationSettings : ISavableEntity
    {
        /// <summary>
        /// Пароль администратора для расширенного управления.
        /// </summary>
        public string? AdminPassword { get; set; }

        public string NameOnSaving => nameof(ApplicationSettings);
    }
}
