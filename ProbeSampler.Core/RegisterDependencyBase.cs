namespace ProbeSampler.Core
{
    /// <summary>
    /// Базовый абстрактный класс для регистрации зависимостей.
    /// </summary>
    public abstract class RegisterDependencyBase
    {
        protected IMutableDependencyResolver mutable = Locator.CurrentMutable;
        protected IReadonlyDependencyResolver resolver = Locator.Current;

        public RegisterDependencyBase()
        {
            ConfigService();
        }

        /// <summary>
        /// Регистрация зависимостей.
        /// </summary>
        public abstract void ConfigService();
    }
}
