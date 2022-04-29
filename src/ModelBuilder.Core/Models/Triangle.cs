using System.Numerics;

namespace ModelBuilder.Core.Models
{
    public class Triangle
    {
        public Vector3 Normal { get; set; }

        public Point FirstPoint { get; set; }

        public Point SecondPoint { get; set; }

        public Point ThirdPoint { get; set; }
        
    }
}
