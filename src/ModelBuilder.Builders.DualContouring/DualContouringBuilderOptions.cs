namespace ModelBuilder.Builders.DualContouring
{
    public class DualContouringBuilderOptions
    {
        public static readonly DualContouringBuilderOptions Default = new()
        {
            CellSize = 1f,
            UseBias = true,
            BiasStrength = 0.01f,
            Boundary = true,
            ClipPointToCellBounds = false,
            ChangePointSelectionMode = ChangePointSelectionMode.LinearApproximation,
            BestPointSelectionAccuracy = 0.01f,
            MaxDegreeOfParallelism = -1,
        };

        public float CellSize { get; set; }

        public bool UseBias { get; set; }

        public float BiasStrength { get; set; }

        public bool Boundary { get; set; }

        public bool ClipPointToCellBounds { get; set; }

        public ChangePointSelectionMode ChangePointSelectionMode { get; set; }

        public float BestPointSelectionAccuracy { get; set; }

        public int MaxDegreeOfParallelism { get; set; }
    }

    public enum ChangePointSelectionMode
    {
        LinearApproximation,
        BestPointSelection,
    }
}
