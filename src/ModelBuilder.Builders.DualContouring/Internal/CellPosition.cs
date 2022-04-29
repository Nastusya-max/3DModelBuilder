using System;

namespace ModelBuilder.Builders.DualContouring.Internal
{
    internal readonly struct CellPosition : IEquatable<CellPosition>
    {
        public CellPosition(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public readonly int X;

        public readonly int Y;

        public readonly int Z;

        public CellPosition Shift(int deltaX, int deltaY, int deltaZ)
        {
            return new CellPosition(X + deltaX, Y + deltaY, Z + deltaZ);
        }

        public override bool Equals(object obj)
        {
            return obj is CellPosition position && Equals(position);
        }

        public bool Equals(CellPosition other)
        {
            return X == other.X &&
                   Y == other.Y &&
                   Z == other.Z;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }
    }
}
