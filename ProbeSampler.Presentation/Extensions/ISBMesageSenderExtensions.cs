namespace ProbeSampler.Presentation.Extensions
{
    public static class ISBMesageSenderExtensions
    {
        /// <summary>
        /// Отправить уведомление по привязаному идентификатору.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message">Объект уведомления <see cref="SBMessage"/>.</param>
        public static void SendMessage(this ISBMessageSender sender, SBMessage message)
        {
            MessageBus.Current.SendMessage(message, sender.Identifier);
        }
    }
}
