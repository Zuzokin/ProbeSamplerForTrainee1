﻿using System.ComponentModel;
using System.IO;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace ProbeSampler.WPF
{
    public class ObservableLogger : Splat.ILogger, IDisposable
    {
        /*private readonly IDisplayLog? _displayLog;*/
        private readonly StreamWriter streamWriter;
        private readonly object lockObj = new();
        private bool disposedValue;

        public static Subject<(string msg, LogLevel level)> LogMsg { get; } = new Subject<(string msg, LogLevel level)>();

        public ObservableLogger()
        {
            /*_displayLog = Locator.Current.GetService<IDisplayLog>();*/
            Task.Run(async () => await ClearLogs());
            streamWriter = InitFile();
        }

        public bool LogToConsole { get; set; } = true;

        public LogLevel Level { get; }

        public void Write(string message, LogLevel logLevel)
        {
            if (logLevel > LogLevel.Debug && !message.Contains("POCO") && !message.Contains("MessageBus"))
            {
                WriteFile(message, logLevel);
                // displayLog?.Log(message, DateTime.Now, logLevel);
                LogMsg.OnNext((message, logLevel));
            }
        }

        public void Write(Exception exception, [Localizable(false)] string message, LogLevel logLevel)
        {
            if (logLevel > LogLevel.Debug && !message.Contains("POCO") && !message.Contains("MessageBus"))
            {
                WriteFile(message, logLevel);
                // displayLog?.Log(message, DateTime.Now, logLevel);
                LogMsg.OnNext((message, logLevel));
            }
        }

        public void Write([Localizable(false)] string message, [Localizable(false)] Type type, LogLevel logLevel)
        {
            if (logLevel > LogLevel.Debug && !message.Contains("POCO") && !message.Contains("MessageBus"))
            {
                WriteFile(message, logLevel);
                // displayLog?.Log(message, DateTime.Now, logLevel);
                LogMsg.OnNext((message, logLevel));
            }
        }

        public void Write(Exception exception, [Localizable(false)] string message, [Localizable(false)] Type type, LogLevel logLevel)
        {
            if (logLevel > LogLevel.Debug && !message.Contains("POCO") && !message.Contains("MessageBus"))
            {
                WriteFile(message, logLevel);
                // displayLog?.Log(message, DateTime.Now, logLevel);
                LogMsg.OnNext((message, logLevel));
            }
        }

        private static StreamWriter InitFile()
        {
            var folder = Path.Combine(Environment.CurrentDirectory, "logs");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var filename = $"{DateTime.Now:yyyy-MM-dd}.txt";
            var filePath = Path.Combine(folder, filename);

            return !File.Exists(filePath) ? File.CreateText(filePath) : File.AppendText(filePath);
        }

        private void WriteFile(string message, LogLevel logLevel)
        {
            if (!string.IsNullOrEmpty(message) && !message.Contains("POCOObservableForProperty"))
            {
                var logText = $"{DateTime.Now:HH:mm:ss}:  {Enum.GetName(logLevel),-6}  {message}";

                if (LogToConsole) // Проверяем флаг перед отправкой в консоль
                {
                    Console.WriteLine(logText);
                }

                lock (lockObj)
                {
                    // var logText = $"{DateTime.Now:HH:mm:ss}:  {Enum.GetName(logLevel),-6}  {message}";
                    streamWriter.WriteLine(logText);
                    streamWriter.Flush();
                }
            }
        }

        private static async Task ClearLogs()
        {
            await Task.Run(() =>
            {
                if (DateTime.Now.DayOfWeek == DayOfWeek.Thursday)
                {
                    var path = Path.Combine(Environment.CurrentDirectory, "logs");
                    var files = Directory.GetFiles(path);
                    foreach (var file in files)
                    {
                        var time = File.GetCreationTime(file);
                        if (DateTime.Now.Subtract(time).TotalDays > 14)
                        {
                            File.Delete(file);
                        }
                    }
                }
            });
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                    streamWriter?.Dispose();
                }

                // TODO: 释放未托管的资源(未托管的对象)并重写终结器
                // TODO: 将大型字段设置为 null
                disposedValue = true;
            }
        }

        // // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器 ~ObservableLogger() { //
        // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中 Dispose(disposing: false); }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
