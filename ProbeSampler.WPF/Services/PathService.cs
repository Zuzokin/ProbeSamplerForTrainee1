using ProbeSampler.Assets;
using System.IO;

namespace ProbeSampler.WPF.Services
{
    public class PathService : IPathService
    {
        public string RootFolderName { get; private set; }

        public string DataPath { get; private set; }

        public string ConfigsPath { get; private set; }

        public string LogsPath { get; private set; }

        public string CapturesPath { get; private set; }

        public string YoloDetectPath { get; private set; }

        public string YoloSegmentPath { get; private set; }

        public string PretrainedYoloDetectPath { get; private set; }

        public string SettingsPath { get; private set; }

        public PathService(string rootFolderNmae = "ProbeSampler.WPF", string? dataPath = default)
        {
            RootFolderName = rootFolderNmae;
            DataPath = dataPath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                RootFolderName);
            ConfigsPath = Path.Combine(DataPath, "Connections");
            LogsPath = Path.Combine(DataPath, "Logs");
            CapturesPath = Path.Combine(DataPath, "Captures");
            YoloDetectPath = FilePath.Yolov8onnx;
            YoloSegmentPath = FilePath.Yolov8segonnx;
            PretrainedYoloDetectPath = FilePath.PreTrainedYolov8detectonnx;
            SettingsPath = Path.Combine(DataPath, "Settings");

            Path.GetDirectoryName(DataPath);
            if (!Directory.Exists(DataPath))
            {
                Directory.CreateDirectory(DataPath);
            }

            if (!Directory.Exists(ConfigsPath))
            {
                Directory.CreateDirectory(ConfigsPath);
            }

            if (!Directory.Exists(LogsPath))
            {
                Directory.CreateDirectory(LogsPath);
            }

            if (!Directory.Exists(CapturesPath))
            {
                Directory.CreateDirectory(CapturesPath);
            }
        }
    }
}
