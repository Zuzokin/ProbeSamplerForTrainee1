using Serilog.Core;
using Serilog.Events;
using System.Globalization;
using System.Reactive.Subjects;

namespace ProbeSampler.WPF
{
    public class ObservableLoggerSink : ILogEventSink
    {
        public static Subject<(string msg, LogLevel level)> LogMsg { get; } = new Subject<(string msg, LogLevel level)>();

        private readonly IFormatProvider formatProvider;

        public ObservableLoggerSink(IFormatProvider? formatProvider = default)
        {
            this.formatProvider = formatProvider ?? CultureInfo.InvariantCulture;
        }

        public void Emit(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage(formatProvider);
            if (logEvent.Level > LogEventLevel.Debug && !message.Contains("POCO") && !message.Contains("MessageBus"))
            {
                LogMsg.OnNext((message, ConvertToSplatLogLevel(logEvent.Level)));
            }
        }

        private LogLevel ConvertToSplatLogLevel(LogEventLevel serilogLevel)
        {
            switch (serilogLevel)
            {
                case LogEventLevel.Debug:
                    return LogLevel.Debug;
                case LogEventLevel.Information:
                    return LogLevel.Info;
                case LogEventLevel.Warning:
                    return LogLevel.Warn;
                case LogEventLevel.Error:
                    return LogLevel.Error;
                case LogEventLevel.Fatal:
                    return LogLevel.Fatal;
                default:
                    return LogLevel.Debug;
            }
        }
    }
}
