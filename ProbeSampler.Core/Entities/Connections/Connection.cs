namespace ProbeSampler.Core.Entities
{
    /// <summary>
    /// Базовая модель информации о подключении.
    /// </summary>
    public abstract class Connection : ISavableEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string NameOnSaving => this.Id.ToString();
    }
}
