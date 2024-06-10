using MaterialDesignThemes.Wpf;

namespace ProbeSampler.WPF.Extensions
{
    public static class MessageQueueExtensions
    {
        public static void Enqueue(this SnackbarMessageQueue messageQueue, SBMessage message, Snackbar snackbar)
        {
            // Отмена текущего активного сообщения, если promote установлен в true
            if (message.Promote && snackbar.IsActive)
            {
                snackbar.IsActive = false;
                messageQueue.Clear();
            }

            // Проверка, есть ли действие у сообщения
            if (string.IsNullOrEmpty(message.ActionCaption))
            {
                messageQueue.Enqueue(message.MessageText, null, null, null, false, true, message.DurationOverride);
            }
            else
            {
                messageQueue.Enqueue(
                    content: message.MessageText,
                    actionContent: message.ActionCaption,
                    actionHandler: (object? argument) =>
                    {
                        message.Action?.Invoke(argument); // Вызов действия, если оно есть
                        snackbar.IsActive = false; // Скрываем Snackbar после выполнения действия
                    },
                    actionArgument: message.ActionArgument ?? new object(),
                    promote: message.Promote,
                    neverConsiderToBeDuplicate: false,
                    durationOverride: message.DurationOverride
                );
            }
        }
    }
}
