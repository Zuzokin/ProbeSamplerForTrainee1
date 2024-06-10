namespace ProbeSampler.Core.Services.Contract
{
    /// <summary>
    /// Менеджер настроек приложения.
    /// </summary>
    public interface IApplicationSettingsManager
    {
        IObservable<ApplicationSettings> ApplicationSettingsObservable { get; }

        void SaveSettings();

        void RemoveSettings();

        ApplicationSettings? GetSettings();

        void UpdateSettings(ApplicationSettings applicationSettings, bool save = false);
    }
}
