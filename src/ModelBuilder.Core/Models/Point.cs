using System;

namespace ModelBuilder.Core.Models
{
    public readonly struct Point : IEquatable<Point>
    {
        public Point(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public readonly float X;

        public readonly float Y;

        public readonly float Z;

        public override bool Equals(object obj)
        {
            return obj is Point point && Equals(point);
        }

        public bool Equals(Point other)
        {
            return X == other.X &&
                   Y == other.Y &&
                   Z == other.Z;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public override string ToString()
        {
            return $"({X}; {Y}; {Z})";
        }

        public static bool operator ==(Point left, Point right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Point left, Point right)
        {
            return !(left == right);
        }
    }
}
