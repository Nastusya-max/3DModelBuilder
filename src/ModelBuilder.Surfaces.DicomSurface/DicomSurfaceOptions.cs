namespace ModelBuilder.Surfaces.DicomSurface
{
    public class DicomSurfaceOptions
    {
        public static readonly DicomSurfaceOptions Default = new()
        {
            ActivationThreshold = 200,
            NormalSelectionAccuracyInsideLayer = 2.5f,
        };

        public byte ActivationThreshold { get; set; }

        public float NormalSelectionAccuracyInsideLayer { get; set; }
    }
}
