namespace ProbeSampler.Presentation.Base
{
    /// <summary>
    /// Tag-интерфейс View для поддержки отправки сообщений.
    /// </summary>
    public interface IIdentifierProvider
    {
        /// <summary>
        /// Идентификатор контракта окна, в котором отобразится сообщение <see cref="SBMessage"/>.
        /// </summary>
        string Identifier { get; }
    }
}
