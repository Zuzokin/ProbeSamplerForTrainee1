namespace ProbeSampler.Presentation.Helpers
{
    public static class LocalizationHelper
    {
        public static string ToDisplayText<T>(this T value)
            where T : Enum
        {
            return value switch
            {
                CameraState cameraState => HandleCameraState(cameraState),
                OPCUAConnectionStatus status => HandleOPCUAConnectionStatus(status),        
                DetectionType detectionType => HandleDetectionType(detectionType),
                CrossbarType crossbarType => HandleCrossbarType(crossbarType),
                DeleteCellsMode deleteCellsMode => HandleDeleteModeType(deleteCellsMode),
                _ => throw new ArgumentException($"No display text logic for enum {value} of type {typeof(T)}"),
            };
        }

        private static string HandleCameraState(CameraState state)
        {
            string result = "Остановлено";

            switch (state)
            {
                case CameraState.Pause:
                    result = "Пауза";
                    break;
                case CameraState.Connecting:
                    result = "Подключение";
                    break;
                case CameraState.Stopped:
                    result = "Остановлено";
                    break;
                case CameraState.Stopping:
                    result = "Остановка";
                    break;
                case CameraState.Running:
                    result = "Воспроизведение";
                    break;
                case CameraState.Error:
                    result = "Ошибка";
                    break;
            }

            return result;
        }

        private static string HandleOPCUAConnectionStatus(OPCUAConnectionStatus status)
        {
            string result = "Неизвестно";

            switch (status)
            {
                case OPCUAConnectionStatus.Connected:
                    result = "Подключено";
                    break;                
                case OPCUAConnectionStatus.None:
                    result = "Неизвестно";
                    break;
                case OPCUAConnectionStatus.Connecting:
                    result = "Подключение";
                    break;
                case OPCUAConnectionStatus.Error:
                    result = "Ошибка, проверьте соеденение с opc сервером";
                    break;
                case OPCUAConnectionStatus.Disconnected:
                    result = "Отключено";
                    break;
                case OPCUAConnectionStatus.ErrorNoConnectionToProbe:
                    result = "Ошибка, проверьте соединение с пробоотборником";
                    break;
                case OPCUAConnectionStatus.ErrorNoConnectionToGate:
                    result = "Ошибка, проверьте соединение с контроллером ворот";
                    break;
                default:
                    result = "Ошибка";
                    break;
            }

            return result;
        }

        private static string HandleDetectionType(DetectionType detectionType)
        {
            string result = "Вариант 1 (OpenCV)";

            switch (detectionType)
            {
                case DetectionType.OpenCVBoxSearch:
                    result = "OpenCV";
                    break;
                case DetectionType.Yolov8:
                    result = "Yolov8";
                    break;
                case DetectionType.None:
                    result = "Отключено";
                    break;
            }

            return result;
        }

        private static string HandleCrossbarType(CrossbarType detectionType)
        {
            string result = "Неопределённо";

            switch (detectionType)
            {
                case CrossbarType.Vertical:
                    result = "Вертикальная";
                    break;
                case CrossbarType.Horizontal:
                    result = "Горизонтальная";
                    break;
                default:
                    result = "Неопределённо";
                    break;
            }

            return result;
        }
        private static string HandleDeleteModeType(DeleteCellsMode deleteModeType)
        {
            string result = "Неопределённо";

            switch (deleteModeType)
            {
                case DeleteCellsMode.Cells:
                    result = "Удалить клетки";
                    break;
                case DeleteCellsMode.Crossbars:
                    result = "Добавить балки";
                    break;
                default:
                    result = "Неопределённо";
                    break;
            }

            return result;
        }
    }
}
