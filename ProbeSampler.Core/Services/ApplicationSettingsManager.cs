using System.Reactive.Linq;

namespace ProbeSampler.Core.Services
{
    public class ApplicationSettingsManager : IApplicationSettingsManager
    {
        private readonly IReadonlyDependencyResolver resolver = Locator.Current;
        private readonly IStorageService storageService;
        private readonly IPathService pathService;

        private ApplicationSettings? applicationSettings => applicationSettingsObservable.Value;

        private BehaviorSubject<ApplicationSettings> applicationSettingsObservable = new BehaviorSubject<ApplicationSettings>(new ApplicationSettings());

        public IObservable<ApplicationSettings> ApplicationSettingsObservable => applicationSettingsObservable.AsObservable();

        public ApplicationSettingsManager(IStorageService? storageService = null)
        {
            this.storageService = storageService ?? resolver.GetRequiredService<IStorageService>();
            pathService = resolver.GetRequiredService<IPathService>();

            ApplicationSettings? settings = null;
            try
            {
                settings = this.storageService.Load<ApplicationSettings>(nameof(Entities.ApplicationSettings), pathService.SettingsPath);
                if (settings == null)
                {
                    settings = new ApplicationSettings();
                    this.storageService.Save(settings, pathService.SettingsPath);
                }
            }
            catch (DirectoryNotFoundException)
            {
                settings = new ApplicationSettings();
                this.storageService.Save(settings, pathService.SettingsPath);
            }

            applicationSettingsObservable.OnNext(settings);
        }

        public void SaveSettings()
        {
            if (applicationSettings != null)
            {
                storageService.Save(applicationSettings, pathService.SettingsPath);
            }
        }

        public void RemoveSettings()
        {
            if (applicationSettings != null)
            {
                storageService.Remove(applicationSettings.NameOnSaving, pathService.SettingsPath);
            }
        }

        public ApplicationSettings? GetSettings()
        {
            return applicationSettings;
        }

        public void UpdateSettings(ApplicationSettings applicationSettings, bool save = false)
        {
            applicationSettingsObservable.OnNext(applicationSettings);
            if (save)
            {
                SaveSettings();
            }
        }
    }
}
