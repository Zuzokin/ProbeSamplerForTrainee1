using Emgu.CV;

namespace ProbeSampler.Core.Services.Processing
{
    public abstract class ImageProcessor : IDisposable
    {
        protected bool isDisposed;

        private int matHeight;
        private int matWidth;

        protected int MatHeight
        {
            get => matHeight;
            set
            {
                if (matHeight != value)
                {
                    matHeight = value;
                }
            }
        }

        protected int MatWidth
        {
            get => matWidth;
            set
            {
                if (matWidth != value)
                {
                    matWidth = value;
                }
            }
        }

        protected abstract Mat OnImageReceived(Mat image);

        public virtual Mat ReceiveImage(Mat image)
        {
            if (MatHeight != image.Rows || MatWidth != image.Width)
            {
                MatHeight = image.Rows;
                MatWidth = image.Width;

                OnMatDimensSet(image);
            }

            var processedMat = OnImageReceived(image);
            return processedMat;
        }

        protected virtual void OnMatDimensSet(Mat image)
        {
        }

        public virtual void Dispose()
        {
            isDisposed = true;
        }
    }
}
