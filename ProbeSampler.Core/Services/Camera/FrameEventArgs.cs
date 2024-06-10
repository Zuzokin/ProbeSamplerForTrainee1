using Emgu.CV;

namespace ProbeSampler.Core.Services.Camera
{
    public class FrameEventArgs : EventArgs
    {
        public Mat Frame { get; }

        public FrameEventArgs(Mat frame)
        {
            Frame = frame;
        }
    }
}
