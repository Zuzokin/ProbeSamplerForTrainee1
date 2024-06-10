using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Drawing;

namespace ProbeSampler.Core.Services.Processing
{
    public class ScaleProcessor : ImageProcessor
    {
        Size? size;

        private double[,] camera_matrix = new double[,]
        {
            { 1.30228070e+03, 0.00000000e+00, 9.29968839e+02 },
            { 0.00000000e+00, 1.34960870e+03, 5.39137137e+02 },
            { 0.00000000e+00, 0.00000000e+00, 1.00000000e+00 },
        };

        private double[] dist_coefs = new double[]
        {
            -4.25983570e-01, 2.44448197e-01, 5.95680220e-05, 1.19554178e-03, -8.09064284e-02,
        };

        private Mat map1 = new();
        private Mat map2 = new();

        Matrix<double> cameraMatrixMatrix;
        Matrix<double> distCoeffsMatrix;

        public ScaleProcessor()
        {
            cameraMatrixMatrix = new Matrix<double>(camera_matrix);
            distCoeffsMatrix = new Matrix<double>(dist_coefs.Length, 1);
            for (int i = 0; i < dist_coefs.Length; i++)
            {
                distCoeffsMatrix[i, 0] = dist_coefs[i];
            }
        }

        protected override Mat OnImageReceived(Mat inputImage)
        {
            if (size != null)
            {
                CvInvoke.Resize(inputImage, inputImage, (Size)size);
            }

            if (map1.IsEmpty && map2.IsEmpty)
            {
                CvInvoke.InitUndistortRectifyMap(cameraMatrixMatrix, distCoeffsMatrix, null, cameraMatrixMatrix, inputImage.Size, DepthType.Cv32F, 1, map1: map1, map2: map2);
            }

            Mat outputImage = new Mat();
            CvInvoke.Remap(inputImage, outputImage, map1, map2, Inter.Linear);

            return outputImage;
        }

        public void SetSize()
        {
            size = new System.Drawing.Size(640, 360);
        }

        public override void Dispose()
        {
            base.Dispose();

            cameraMatrixMatrix.Dispose();
            distCoeffsMatrix.Dispose();
            map1.Dispose();
            map2.Dispose();
        }
    }
}
