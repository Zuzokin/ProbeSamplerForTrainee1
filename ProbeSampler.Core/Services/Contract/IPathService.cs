namespace ProbeSampler.Core.Services.Contract
{
    /// <summary>
    /// Менеджер путей.
    /// </summary>
    public interface IPathService
    {
        /// <summary>
        /// Наименование корневой папки приложения.
        /// </summary>
        string RootFolderName { get; }
        /// <summary>
        /// Путь до папки данных приложения.
        /// </summary>
        string DataPath { get; }
        /// <summary>
        /// Путь до конфигураций.
        /// </summary>
        string ConfigsPath { get; }
        /// <summary>
        /// Путь до настроек приложения.
        /// </summary>
        string SettingsPath { get; }
        /// <summary>
        /// Путь до логов.
        /// </summary>
        string LogsPath { get; }

        /// <summary>
        /// Путь до захваченных кадров.
        /// </summary>
        string CapturesPath { get; }
        /// <summary>
        /// Путь до detection модели YOLOv8.
        /// </summary>
        string YoloDetectPath { get; }
        /// <summary>
        /// Путь до segmetation модели YOLOv8.
        /// </summary>
        string YoloSegmentPath { get; }
        /// <summary>
        /// Путь до преобученной модели YOLOv8.
        /// </summary>
        string PretrainedYoloDetectPath { get; }
    }
}
