using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ModelBuilder.Surfaces.DicomSurface.Internal
{
    internal class DicomLayer
    {
        private readonly Image<Bgra32> _image;
        private byte _activationThreshold;

        public DicomLayer(Image<Bgra32> image)
        {
            _image = image;
        }

        public float Width => _image.Width * PixelSpacing.X;

        public float Height => _image.Height * PixelSpacing.Y;

        public float Thickness { get; init; }

        public float Offset { get; init; }

        public (float X, float Y) PixelSpacing { get; init; }

        internal void Initialize(float width, float height, DicomSurfaceOptions options)
        {
            _activationThreshold = options.ActivationThreshold;

            if (Width == width && Height == height)
            {
                return;
            }

            var pixelWidth = (int)(width / PixelSpacing.X);
            var pixelHeight = (int)(height / PixelSpacing.Y);

            _image.Mutate(x => x.Grayscale().Resize(new ResizeOptions
            {
                Size = new Size(pixelWidth, pixelHeight),
                Mode = ResizeMode.BoxPad,
            }));
        }

        internal float GetValue(float x, float y)
        {
            var scaledX = (int)(x / PixelSpacing.X);
            var scaledY = (int)(y / PixelSpacing.Y);

            return _activationThreshold - _image[scaledX, scaledY].R;
        }
    }
}
