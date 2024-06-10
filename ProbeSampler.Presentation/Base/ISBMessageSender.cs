namespace ProbeSampler.Presentation.Base
{
    /// <summary>
    /// Tag-интерфейс ViewModel для поддержки отправки сообщений.
    /// </summary>
    public interface ISBMessageSender
    {
        /// <summary>
        /// Идентификатор контракта окна, в котором отобразится сообщение <see cref="SBMessage"/>.
        /// </summary>
        string Identifier { get; }
    }
}
