namespace ProbeSampler.Presentation.Extensions
{
    public static class INestedMessageSenderExtensions
    {
        /// <summary>
        /// Отправить уведомление в текущее окно.
        /// </summary>
        /// <param name="nestedSender"></param>
        /// <param name="message">Текст уведомления.</param>
        /// <param name="promote">Приоритет уведомления, по умолчанию true - вывод поверх остальных уведомлений.</param>
        public static void SendMessageToBus(this INestedMessageSender nestedSender, string message, bool promote = true)
        {
            nestedSender.SendMessageToBus(new SBMessage(message, promote));
        }

        /// <summary>
        /// Отправить уведомление в текущее окно.
        /// </summary>
        /// <param name="nestedSender"></param>
        /// <param name="message">Объект уведомления <see cref="SBMessage"/>.</param>
        public static void SendMessageToBus(this INestedMessageSender nestedSender, SBMessage message)
        {
            if (nestedSender.MessageSender != null)
            {
                nestedSender.MessageSender.SendMessage(message);
            }
            else
            {
                MessageBus.Current.SendMessageToMainApplicationWindow(message);
            }
        }
    }
}
