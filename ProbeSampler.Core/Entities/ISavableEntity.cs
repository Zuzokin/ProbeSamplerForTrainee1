namespace ProbeSampler.Core.Entities
{
    /// <summary>
    /// Интерфейс для сохраняемых сущностей.
    /// </summary>
    public interface ISavableEntity
    {
        /// <summary>
        /// Наименование при сохранении.
        /// </summary>
        string NameOnSaving { get; }
    }
}
