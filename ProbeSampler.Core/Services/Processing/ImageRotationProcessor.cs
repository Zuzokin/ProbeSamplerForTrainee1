using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Drawing;

namespace ProbeSampler.Core.Services.Processing
{
    public class ImageRotationProcessor : ImageProcessor
    {
        Mat? mapX;
        Mat? mapY;
        double angle;

        public double Angle
        {
            get => angle;
            set
            {
                if (angle != value)
                {
                    angle = value;
                    FillMaps();
                }
            }
        }

        protected override Mat OnImageReceived(Mat image)
        {
            Mat resultImage = image.Clone();

            if (!isDisposed && Angle != 0 && mapX != null && !mapX.IsEmpty && mapY != null && !mapY.IsEmpty)
            {
                CvInvoke.Remap(image, resultImage, mapX, mapY, Inter.Linear, BorderType.Replicate, new MCvScalar(0));
            }

            return resultImage;
        }

        protected override void OnMatDimensSet(Mat image)
        {
            base.OnMatDimensSet(image);

            mapX = new Mat(MatHeight, MatWidth, DepthType.Cv32F, 1);
            mapY = new Mat(MatHeight, MatWidth, DepthType.Cv32F, 1);

            FillMaps();
        }

        private void FillMaps()
        {
            if (mapX == null || mapY == null)
            {
                return;
            }

            PointF center = new PointF(MatWidth / 2.0F, MatHeight / 2.0F);

            Image<Gray, float> mapXImage = mapX.ToImage<Gray, float>();
            Image<Gray, float> mapYImage = mapY.ToImage<Gray, float>();

            double radAngle = angle * (Math.PI / 180.0);

            for (int y = 0; y < MatHeight; y++)
            {
                for (int x = 0; x < MatWidth; x++)
                {
                    float newX = (float)((Math.Cos(radAngle) * (x - center.X)) - (Math.Sin(radAngle) * (y - center.Y)) + center.X);
                    float newY = (float)((Math.Sin(radAngle) * (x - center.X)) + (Math.Cos(radAngle) * (y - center.Y)) + center.Y);

                    mapXImage.Data[y, x, 0] = newX;
                    mapYImage.Data[y, x, 0] = newY;
                }
            }

            mapX = mapXImage.Mat;
            mapY = mapYImage.Mat;
        }

        public override void Dispose()
        {
            base.Dispose();

            mapX?.Dispose();
            mapY?.Dispose();
        }
    }
}
