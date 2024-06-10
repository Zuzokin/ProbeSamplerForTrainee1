namespace ProbeSampler.Core.Entities
{
    /// <summary>
    /// Подключение к камере.
    /// </summary>
    public class CameraConnection : Connection, ICloneable
    {
        public string? URL { get; set; }
        /// <summary>
        /// Исходная ширина кадра в пискелях.
        /// </summary>
        public int InputWidth { get; set; }
        /// <summary>
        /// Исходная высота кадра в пискелях.
        /// </summary>
        public int InputHeight { get; set; }
        /// <summary>
        /// Высота обзора.
        /// </summary>
        public double ViewHeight { get; set; }
        /// <summary>
        /// Ширина обзора.
        /// </summary>
        public double ViewWidth { get; set; }
        /// <summary>
        /// Высота сетки.
        /// </summary>
        public double GridOverlayHeight { get; set; }
        /// <summary>
        /// Ширина сетки.
        /// </summary>
        public double GridOverlayWidth { get; set; }
        /// <summary>
        /// Начальные координаты по X (Отсчитываются от верхнего левого угла).
        /// </summary>
        public double GridOverlayX { get; set; }
        /// <summary>
        /// Начальные координаты по Y (Отсчитываются от верхнего левого угла).
        /// </summary>
        public double GridOverlayY { get; set; }
        /// <summary>
        /// Размеры ячейки сетки.
        /// </summary>
        public int GridOverlayCellHeight { get; set; }

        /// <summary>
        /// Матрица камеры.
        /// </summary>
        public double[,]? CameraMatrix { get; set; }

        /// <summary>
        /// Коэфициент дисторшена.
        /// </summary>
        public double[,]? DistCoeff { get; set; }

        /// <summary>
        /// Значение для калибровки, используется для рассчета newCameraMatrix.
        /// </summary>
        public double Alpha { get; set; }

        /// <summary>
        /// Угол наклона.
        /// </summary>
        public double RotationAngle { get; set; }

        /// <summary>
        /// Масштаб.
        /// </summary>
        public double Scale { get; set; }

        public object Clone()
        {
            var clone = new CameraConnection
            {
                Id = Id,
                URL = URL,
                InputWidth = InputWidth,
                InputHeight = InputHeight,
                ViewHeight = ViewHeight,
                ViewWidth = ViewWidth,
                GridOverlayHeight = GridOverlayHeight,
                GridOverlayWidth = GridOverlayWidth,
                GridOverlayX = GridOverlayX,
                GridOverlayY = GridOverlayY,
                GridOverlayCellHeight = GridOverlayCellHeight,
                CameraMatrix = CameraMatrix,
                DistCoeff = DistCoeff,
                Alpha = Alpha,
                RotationAngle = RotationAngle,
                Scale = Scale,
            };

            return clone;
        }
    }
}
