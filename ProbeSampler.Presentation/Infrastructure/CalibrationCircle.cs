using ProbeSampler.Presentation.Helpers;

namespace ProbeSampler.Presentation
{
    /// <summary>
    /// Круг отображаемый поверх, изображения, которой служит для проверки калибровки
    /// </summary>
    public class CalibrationCircle
    {
        /// <summary>
        /// Координата X (px)
        /// </summary>
        public double X { get; private set; }
        /// <summary>
        /// Координата Y (px)
        /// </summary>
        public double Y { get; private set; }
        /// <summary>
        /// Ширина круга
        /// </summary>
        public double Width { get; private set; }
        /// <summary>
        /// Высота круга
        /// </summary>
        public double Height { get; private set; }

        public double Rotation { get; private set; }

        public double Offset { get; private set; }

        public CalibrationCircle(double x, double y, double width = 50, double height = 50, object? relatedObj = default)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public CalibrationCircle CalculateValuesForOpc(ConnectionConfiguration connectionConfiguration)
        {
            if (connectionConfiguration is not null)
            {
                Rotation = ConvectorHelper.ConvertPixelsYToRotation(
                    yPixels: Y,
                    a: connectionConfiguration.SamplerConnection.RotationCalculationCoeffA,
                    b: connectionConfiguration.SamplerConnection.RotationCalculationCoeffB,
                    c: connectionConfiguration.SamplerConnection.RotationCalculationCoeffC,
                    isOver90Degrees: false);

                Offset = ConvectorHelper.ConvertPixelsXToLinearDisplacement(
                    xPixels: X,
                    a: connectionConfiguration.SamplerConnection.LinearCalculationCoeffA,
                    b: connectionConfiguration.SamplerConnection.LinearCalculationCoeffB,
                    CorrectionValue: ConvectorHelper.CalculateCorrectionValue(
                        rotation: Rotation,
                        beakCoeff: connectionConfiguration.SamplerConnection.BeakCoeff),
                    isOver90Degrees: false);
            }

            return this;
        }
    }
}
