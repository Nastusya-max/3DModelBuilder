using ModelBuilder.Core.Models;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<float>;
using Matrix = MathNet.Numerics.LinearAlgebra.Matrix<float>;
using System.Collections.Generic;
using System.Numerics;
using System;

namespace ModelBuilder.Builders.DualContouring.Internal
{
    internal class QefSolver
    {
        private readonly Matrix _matrix;
        private readonly Vector _vector;
        private readonly Vector _massPoint;
        private readonly float?[] _constraints;

        private QefSolver(Matrix matrix, Vector vector, Vector massPoint, float?[] constraints)
        {
            _matrix = matrix;
            _vector = vector;
            _constraints = constraints;
            _massPoint = massPoint;
        }

        public QefSolver(List<Point> points, List<Vector3> normals)
        {
            _massPoint = GetMassPoint(points);
            _matrix = Matrix.Build.Dense(normals.Count, 3, (x, y) => GetVectorElement(normals[x], y));
            _vector = Vector.Build.Dense(normals.Count, x => (points[x].X - _massPoint[0]) * normals[x].X +
                                                             (points[x].Y - _massPoint[1]) * normals[x].Y +
                                                             (points[x].Z - _massPoint[2]) * normals[x].Z);
        }

        public QefSolver FixAxis(Axis axis, float value)
        {
            var vector = Vector.Build.Dense(_vector.Count);
            for (int i = 0; i < _vector.Count; i++)
            {
                vector[i] = _vector[i] - _matrix[i, (int)axis] * (value - _massPoint[(int)axis]);
            }

            var matrix = _matrix.RemoveColumn((int)axis);
            var massPoint = Vector.Build.Dense(_massPoint.Count - 1, x => x >= (int)axis ? _massPoint[x + 1] : _massPoint[x]);
            if (_constraints == null)
            {
                return new QefSolver(matrix, vector, massPoint, MapAxisToArray(axis, value));
            }

            return new QefSolver(matrix, vector, massPoint, CopyConstraintsWithNewAxis(axis, value));
        }

        public Point Solve()
        {
            var result = SolveInternal();
            return MapVectorToPoint(result);
        }

        public double CalculateErrorInPoint(in Point point)
        {
            var vector = Vector.Build.Dense(new[] { point.X, point.Y, point.Z });
            return CalculateErrorInternal(vector);
        }

        public (Point point, double error) SolveWithError()
        {
            var result = SolveInternal();
            var error = CalculateErrorInternal(result);

            return (MapVectorToPoint(result), error);
        }

        private static Vector GetMassPoint(List<Point> points)
        {
            var vector = Vector.Build.Dense(3);
            for (int i = 0; i < points.Count; i++)
            {
                vector[0] += points[i].X;
                vector[1] += points[i].Y;
                vector[2] += points[i].Z;
            }

            vector /= points.Count;

            return vector;
        }

        private static float GetVectorElement(in Vector3 vector, int index)
        {
            if (index == 0)
            {
                return vector.X;
            }

            if (index == 1)
            {
                return vector.Y;
            }

            if (index == 2)
            {
                return vector.Z;
            }

            throw new ArgumentException("Index was outside of the vector elements", nameof(index));
        }

        private static float?[] MapAxisToArray(Axis axis, float value)
        {
            if (axis == Axis.X)
            {
                return new float?[] { value, null, null };
            }

            if (axis == Axis.Y)
            {
                return new float?[] { null, value, null };
            }

            if (axis == Axis.Z)
            {
                return new float?[] { null, null, value };
            }

            throw new ArgumentException("Incorrect axis value", nameof(axis));
        }

        private float?[] CopyConstraintsWithNewAxis(Axis axis, float value)
        {
            var result = (float?[])_constraints.Clone();
            if (axis == Axis.X)
            {
                result[0] = value;
                return result;
            }

            if (axis == Axis.Y)
            {
                result[1] = value;
                return result;
            }

            if (axis == Axis.Z)
            {
                result[2] = value;
                return result;
            }

            throw new ArgumentException("Incorrect axis value", nameof(axis));
        }

        private Vector SolveInternal()
        {
            var transposed = _matrix.Transpose();
            var ata = transposed * _matrix;
            var atb = transposed * _vector;

            return ata.PseudoInverse() * atb + _massPoint;
        }

        private double CalculateErrorInternal(Vector point)
        {
            var axb = _matrix * point - _vector;
            return axb.L2Norm();
        }

        private Point MapVectorToPoint(Vector vector)
        {
            if (_constraints == null)
            {
                return new Point(vector[0], vector[1], vector[2]);
            }

            Span<float> result = stackalloc float[3];
            var k = 0;
            for (int i = 0; i < 3; i++)
            {
                if (_constraints[i].HasValue)
                {
                    result[i] = _constraints[i].Value;
                }
                else
                {
                    result[i] = vector[k++];
                }
            }

            return new Point(result[0], result[1], result[2]);
        }
    }
}
