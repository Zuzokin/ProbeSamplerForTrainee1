namespace ProbeSampler.Presentation.Extensions
{
    public static class IMessageBusExtensions
    {
        /// <summary>
        /// Отправить сообщение в главное окно приложения.
        /// </summary>
        /// <param name="messageBus"></param>
        /// <param name="message"></param>
        public static void SendMessageToMainApplicationWindow(this IMessageBus messageBus, SBMessage message)
        {
            messageBus.SendMessage(message, "MainApplicationWindow");
        }

        /// <summary>
        /// Отправить сообщение в главное окно приложения.
        /// </summary>
        /// <param name="messageBus"></param>
        /// <param name="message"></param>
        public static void SendMessageToMainApplicationWindow(this IMessageBus messageBus, string message)
        {
            messageBus.SendMessageToMainApplicationWindow(new SBMessage(message));
        }
    }
}
