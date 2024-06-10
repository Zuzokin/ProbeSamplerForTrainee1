using Dragablz;
using Serilog;
using Splat.Serilog;
using System.IO;
using System.Windows;

namespace ProbeSampler.WPF
{
    public partial class App : Application, IEnableLogger
    {
        public static ItemActionCallback? ClosingItemCallback { get; private set; }

        private WindowsInstanceManager? windowsInstanceManager;

        public App()
        {
            RxApp.SuppressViewCommandBindingMessage = true;

            Locator.CurrentMutable.RegisterLazySingleton(() => new PathService(), typeof(IPathService));
            Locator.CurrentMutable.RegisterLazySingleton(() => new WindowsInstanceManager());

            var pathService = Locator.Current.GetRequiredService<IPathService>();

            CleanupOldLogFiles(pathService.LogsPath, 30);

            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .Filter.ByExcluding("Contains(@Message, 'POCO') or Contains(@Message, 'MessageBus')")
                .WriteTo.File(Path.Combine(pathService.LogsPath, "appLog.log"), rollingInterval: RollingInterval.Day,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Debug(
                    outputTemplate: "{Timestamp:HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}")
                .WriteTo.Sink(new ObservableLoggerSink())
                .CreateLogger();

            Locator.CurrentMutable.UseSerilogFullLogger();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            windowsInstanceManager = Locator.Current.GetRequiredService<WindowsInstanceManager>();

            ClosingItemCallback = OnItemClosingHandler;

            MainWindow mainWindow = windowsInstanceManager.MainWindow;
            mainWindow.Show();

            base.OnStartup(e);

            this.Log().Info("ApplicationInitialized.");
        }

        private static void OnItemClosingHandler(ItemActionCallbackArgs<TabablzControl> args)
        {
            (args.DragablzItem.DataContext as IDisposable)?.Dispose();
        }

        public void CleanupOldLogFiles(string logDirectory, int daysToKeep)
        {
            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
            foreach (var file in new DirectoryInfo(logDirectory).GetFiles("appLog*.log"))
            {
                if (file.CreationTime < cutoffDate)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
