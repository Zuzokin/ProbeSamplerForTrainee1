using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Runtime.InteropServices;

namespace ProbeSampler.Core.Extensions
{
    public static class MatExtensions
    {
        public static double[,] ToArray(this Mat mat)
        {
            int rows = mat.Rows;
            int cols = mat.Cols;
            double[,] array = new double[rows, cols];

            IntPtr dataPtr = mat.DataPointer;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    array[i, j] = Marshal.PtrToStructure<double>(dataPtr + (((i * cols) + j) * sizeof(double)));
                }
            }

            return array;
        }

        public static void SetMatrixValues(this Mat mat, double[,] values)
        {
            if (mat.Depth != DepthType.Cv64F || mat.NumberOfChannels != 1)
            {
                throw new ArgumentException("Matrix should have a depth of 64-bit float and one channel.");
            }

            int rows = values.GetLength(0);
            int cols = values.GetLength(1);

            if (mat.Rows != rows || mat.Cols != cols)
            {
                throw new ArgumentException("Matrix dimensions do not match the provided values.");
            }

            var data = mat.DataPointer;
            unsafe
            {
                double* rowPtr = (double*)data;
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        rowPtr[j] = values[i, j];
                    }

                    rowPtr += cols;
                }
            }
        }

        public static byte[] ConvertMatToByteArray(this Mat mat)
        {
            int dataSize = mat.Rows * mat.Cols * mat.ElementSize;
            byte[] data = new byte[dataSize];
            Marshal.Copy(mat.DataPointer, data, 0, dataSize);
            return data;
        }
    }
}
