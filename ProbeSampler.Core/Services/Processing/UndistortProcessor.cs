using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Drawing;

namespace ProbeSampler.Core.Services.Processing
{
    public class UndistortProcessor : ImageProcessor
    {
        private readonly Mat map1;
        private readonly Mat map2;
        private Mat distCoeffs;
        private Mat cameraMatrix;
        Rectangle validPixROI = new Rectangle();

        private double alpha = -1;

        public double Alpha
        {
            get => alpha;
            set
            {
                if (value != alpha)
                {
                    alpha = value;
                    FillMaps();
                }
            }
        }

        private double[,]? distCoeffsArray;

        public double[,]? DistCoeffsArray
        {
            get => distCoeffsArray;
            set
            {
                if (value != distCoeffsArray)
                {
                    distCoeffsArray = value;
                    if (distCoeffsArray != null)
                    {
                        distCoeffs = new Mat(distCoeffsArray.GetLength(0), distCoeffsArray.GetLength(1), DepthType.Cv64F, 1);
                        distCoeffs.SetMatrixValues(distCoeffsArray);
                    }

                    FillMaps();
                }
            }
        }

        private double[,]? cameraMatrixArray;

        public double[,]? CameraMatrixArray
        {
            get => cameraMatrixArray;
            set
            {
                if (value != cameraMatrixArray)
                {
                    cameraMatrixArray = value;
                    if (cameraMatrixArray != null)
                    {
                        cameraMatrix = new Mat(cameraMatrixArray.GetLength(0), cameraMatrixArray.GetLength(1), DepthType.Cv64F, 1);
                        cameraMatrix.SetMatrixValues(cameraMatrixArray);
                    }

                    FillMaps();
                }
            }
        }

        private Size imageSize
        {
            get
            {
                return new Size(MatWidth, MatHeight);
            }
        }

        public UndistortProcessor()
        {
            map1 = new Mat();
            map2 = new Mat();
            distCoeffs = new Mat();
            cameraMatrix = new Mat();
        }

        protected override Mat OnImageReceived(Mat image)
        {
            Mat outputImage = new Mat();
            if (!isDisposed && !map1.IsEmpty && !map2.IsEmpty)
            {
                // Cuda version
                /*                GpuMat gpuMat = new GpuMat(image.Clone());
                                GpuMat outputGpuMat = new GpuMat();
                                CudaInvoke.Remap(gpuMat, outputGpuMat, map1, map2, Inter.Linear);
                                outputGpuMat.Download(outputImage);*/
                CvInvoke.Remap(image, outputImage, map1, map2, Inter.Linear);
            }
            else
            {
                outputImage = image.Clone();
            }

            return outputImage;
        }

        protected override void OnMatDimensSet(Mat image)
        {
            base.OnMatDimensSet(image);
            FillMaps();
        }

        private void FillMaps()
        {
            if (distCoeffsArray != null && cameraMatrixArray != null && alpha >= 0 && alpha <= 1 && imageSize.Width > 0 && imageSize.Height > 0)
            {
                Mat newCameraMatrix = CvInvoke.GetOptimalNewCameraMatrix(cameraMatrix, distCoeffs, imageSize, Alpha, imageSize, ref validPixROI, false);
                CvInvoke.InitUndistortRectifyMap(cameraMatrix, distCoeffs, null, newCameraMatrix, imageSize, DepthType.Cv32F, 1, map1: map1, map2: map2);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            GC.SuppressFinalize(this);
            map1.Dispose();
            map2.Dispose();
            distCoeffs.Dispose();
            cameraMatrix.Dispose();
        }
    }
}
