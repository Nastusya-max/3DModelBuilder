using System.Runtime.InteropServices;

namespace ModelBuilder.Builders.DualContouring.MeshRepairing
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct Vertex
    {
        public Vertex(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public readonly float X;

        public readonly float Y;

        public readonly float Z;
    }
}
