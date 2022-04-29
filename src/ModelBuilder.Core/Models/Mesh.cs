using System.Collections.Generic;

namespace ModelBuilder.Core.Models
{
    public class Mesh
    {
        public List<Point> Vertices { get; set; }

        public List<int> Indices { get; set; }
    }
}
