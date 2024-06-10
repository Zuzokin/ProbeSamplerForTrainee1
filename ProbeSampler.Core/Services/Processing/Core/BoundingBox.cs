using System.Drawing;

namespace ProbeSampler.Core.Services.Processing.Core
{
    public struct BoundingBox
    {
        public Rectangle Box;
        public float Confidence;
        public string Label;
        public float Width;
        public float Height;
    }
}
