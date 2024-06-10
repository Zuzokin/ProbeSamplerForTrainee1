namespace ProbeSampler.Presentation
{
    /// <summary>
    /// SnackBar сообщение.
    /// </summary>
    public class SBMessage
    {
        public string MessageText { get; private set; }
        /// <summary>
        /// Подпись действия в сообщении.
        /// </summary>
        public string? ActionCaption { get; private set; }
        /// <summary>
        /// Действие, которое будет выполнено при нажатии на кнопку справа в сообщении.
        /// </summary>
        public Action<object?>? Action { get; private set; }
        /// <summary>
        /// Аргумент, который будет пердан в <see cref="SBMessage.Action"/>.
        /// </summary>
        public object? ActionArgument { get; private set; }
        /// <summary>
        /// Время отображения сообщения, после чего оно само скроется.
        /// </summary>
        public TimeSpan? DurationOverride { get; private set; }
        /// <summary>
        /// Показывать сообщение поверх всех остальных.
        /// </summary>
        public bool Promote { get; private set; }

        public SBMessage(string messageText, bool promote = true)
        {
            MessageText = messageText;
            Promote = promote;
        }

        public SBMessage(
            string messageText,
            string actionCaption,
            Action<object?>? action = null,
            object? actionArgument = null,
            bool promote = true,
            TimeSpan? durationOverride = null)
        {
            MessageText = messageText;
            ActionCaption = actionCaption;
            Action = action;
            ActionArgument = actionArgument;
            Promote = promote;
            DurationOverride = durationOverride;
        }
    }
}
