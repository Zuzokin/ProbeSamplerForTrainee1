using Emgu.CV.Structure;
using System.Drawing;

namespace ProbeSampler.Core.Entities
{
    public class ColoredRect
    {
        private Rectangle rect;
        private MCvScalar color;
        private MachinePartType partType;

        public ColoredRect()
        {
        }

        public ColoredRect(Rectangle rect, MCvScalar color)
        {
            this.rect = rect;
            this.color = color;
        }

        public ColoredRect(Rectangle rect, MCvScalar color, MachinePartType partType)
            : this(rect, color)
        {
            PartType = partType;
        }

        public DateTime LastDetected { get; set; }

        public Rectangle Rect { get => rect; set => rect = value; }

        public MCvScalar Color { get => color; set => color = value; }

        public MachinePartType PartType { get => partType; set => partType = value; }

        public override bool Equals(object? obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            ColoredRect other = (ColoredRect)obj;
            return rect.Equals(other.rect);
        }

        public override int GetHashCode()
        {
            return rect.GetHashCode();
        }
    }
}
