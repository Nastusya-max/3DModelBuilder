namespace ModelBuilder.Core.Models
{
    public class BuildBounds
    {
        public static readonly BuildBounds Default = new()
        {
            MinX = -1.0f,
            MaxX = 1.0f,
            MinY = -1.0f,
            MaxY = 1.0f,
            MinZ = -1.0f,
            MaxZ = 1.0f,
        };

        public float MinX { get; set; }

        public float MaxX { get; set; }

        public float MinY { get; set; }

        public float MaxY { get; set; }

        public float MinZ { get; set; }

        public float MaxZ { get; set; }
    }
}
