using ModelBuilder.Builders.DualContouring.Internal;
using ModelBuilder.Core.Interfaces;
using ModelBuilder.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using PoolPointError = System.Buffers.ArrayPool<(ModelBuilder.Core.Models.Point Point, double Error)>;
using PoolPointValue = System.Buffers.ArrayPool<(ModelBuilder.Core.Models.Point Point, float Value)>;

namespace ModelBuilder.Builders.DualContouring
{
    public class DualContouringBuilder : IMeshBuilder
    {
        private static readonly int[] EnumerationRange = new int[] { 0, 1, };

        private readonly DualContouringBuilderOptions _options;

        public DualContouringBuilder(DualContouringBuilderOptions options)
        {
            _options = options;
        }

        public Mesh Build(ISurfaceFunction function, BuildBounds bounds)
        {
            var indicesMap = new ConcurrentDictionary<CellPosition, ConcurrentIndex>();

            var vertices = new List<Point>();
            var indices = new List<int>();

            var probingCellPositions = GetProbingCellPositions(bounds);
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = _options.MaxDegreeOfParallelism,
            };

            Parallel.ForEach(probingCellPositions, parallelOptions, position =>
            {
                var quad = new ValueTuple<int, int, int, int>();
                if (position.X > 0 && position.Y > 0)
                {
                    var firstPoint = CreatePointByCellPosition(position, bounds);
                    var secondPoint = CreatePointByCellPosition(position.Shift(0, 0, 1), bounds);

                    var isFirstPointInside = function.GetValue(firstPoint) < 0.0f;
                    var isSecondPointInside = function.GetValue(secondPoint) < 0.0f;
                    if (isFirstPointInside != isSecondPointInside)
                    {
                        quad.Item1 = GetPointIndex(function, bounds, position.Shift(-1, -1, 0), vertices, indicesMap);
                        quad.Item2 = GetPointIndex(function, bounds, position.Shift(0, -1, 0), vertices, indicesMap);
                        quad.Item3 = GetPointIndex(function, bounds, position, vertices, indicesMap);
                        quad.Item4 = GetPointIndex(function, bounds, position.Shift(-1, 0, 0), vertices, indicesMap);
                        AppendQuadIndices(indices, quad, isSecondPointInside);
                    }
                }

                if (position.X > 0 && position.Z > 0)
                {
                    var firstPoint = CreatePointByCellPosition(position, bounds);
                    var secondPoint = CreatePointByCellPosition(position.Shift(0, 1, 0), bounds);

                    var isFirstPointInside = function.GetValue(firstPoint) < 0.0f;
                    var isSecondPointInside = function.GetValue(secondPoint) < 0.0f;
                    if (isFirstPointInside != isSecondPointInside)
                    {
                        quad.Item1 = GetPointIndex(function, bounds, position.Shift(-1, 0, -1), vertices, indicesMap);
                        quad.Item2 = GetPointIndex(function, bounds, position.Shift(0, 0, -1), vertices, indicesMap);
                        quad.Item3 = GetPointIndex(function, bounds, position, vertices, indicesMap);
                        quad.Item4 = GetPointIndex(function, bounds, position.Shift(-1, 0, 0), vertices, indicesMap);
                        AppendQuadIndices(indices, quad, isFirstPointInside);
                    }
                }

                if (position.Y > 0 && position.Z > 0)
                {
                    var firstPoint = CreatePointByCellPosition(position, bounds);
                    var secondPoint = CreatePointByCellPosition(position.Shift(1, 0, 0), bounds);

                    var isFirstPointInside = function.GetValue(firstPoint) < 0.0f;
                    var isSecondPointInside = function.GetValue(secondPoint) < 0.0f;
                    if (isFirstPointInside != isSecondPointInside)
                    {
                        quad.Item1 = GetPointIndex(function, bounds, position.Shift(0, -1, -1), vertices, indicesMap);
                        quad.Item2 = GetPointIndex(function, bounds, position.Shift(0, 0, -1), vertices, indicesMap);
                        quad.Item3 = GetPointIndex(function, bounds, position, vertices, indicesMap);
                        quad.Item4 = GetPointIndex(function, bounds, position.Shift(0, -1, 0), vertices, indicesMap);
                        AppendQuadIndices(indices, quad, isSecondPointInside);
                    }
                }
            });

            var utilities = new MeshUtilities(vertices, indices);
            utilities.RepairMesh();

            return utilities.Mesh;
        }

        private static void AppendQuadIndices(List<int> indices, ValueTuple<int, int, int, int> quad, bool shouldReverse)
        {
            lock (indices)
            {
                if (shouldReverse)
                {
                    indices.Add(quad.Item1);
                    indices.Add(quad.Item2);
                    indices.Add(quad.Item3);

                    indices.Add(quad.Item1);
                    indices.Add(quad.Item3);
                    indices.Add(quad.Item4);
                    return;
                }

                indices.Add(quad.Item4);
                indices.Add(quad.Item3);
                indices.Add(quad.Item2);

                indices.Add(quad.Item4);
                indices.Add(quad.Item2);
                indices.Add(quad.Item1);
            }
        }

        private static bool IsPointInsideCell(in Point point, in Point min, in Point max)
        {
            return point.X >= min.X && point.X <= max.X &&
                point.Y >= min.Y && point.Y <= max.Y &&
                point.Z >= min.Z && point.Z <= max.Z;
        }

        private static float ClipValue(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private static bool CheckQefList((Point point, double error)[] list, int size, out Point point, in Point min, in Point max)
        {
            var minIndex = -1;
            var minError = 0.0;

            for (int i = 0; i < size; i++)
            {
                if (IsPointInsideCell(list[i].point, min, max))
                {
                    var error = list[i].error;
                    if (minIndex < 0)
                    {
                        minIndex = i;
                        minError = error;
                    }
                    else if(error < minError)
                    {
                        minIndex = i;
                    }
                }
            }

            if (minIndex >= 0)
            {
                point = list[minIndex].point;
                return true;
            }

            point = default;
            return false;
        }

        private static Point GetMiddlePoint(in Point previousPoint, in Point nextPoint, Axis axis)
        {
            if (axis == Axis.X)
            {
                return new Point(previousPoint.X + ((nextPoint.X - previousPoint.X) / 2.0f), previousPoint.Y, previousPoint.Z);
            }
            else if (axis == Axis.Y)
            {
                return new Point(previousPoint.X, previousPoint.Y + ((nextPoint.Y - previousPoint.Y) / 2.0f), previousPoint.Z);
            }
            else
            {
                return new Point(previousPoint.X, previousPoint.Y, previousPoint.Z + ((nextPoint.Z - previousPoint.Z) / 2.0f));
            }
        }

        private IEnumerable<CellPosition> GetProbingCellPositions(BuildBounds bounds)
        {
            var gridWidth = (int)((bounds.MaxX - bounds.MinX) / _options.CellSize);
            var gridHeight = (int)((bounds.MaxY - bounds.MinY) / _options.CellSize);
            var gridDepth = (int)((bounds.MaxZ - bounds.MinZ) / _options.CellSize);

            var rangeX = Enumerable.Range(0, gridWidth);
            var rangeY = Enumerable.Range(0, gridHeight);
            var rangeZ = Enumerable.Range(0, gridDepth);

            return rangeX.SelectMany(x => rangeY.SelectMany(y => rangeZ, (y, z) => ValueTuple.Create(y, z)), (x, yz) => new CellPosition(x, yz.Item1, yz.Item2));
        }

        private Point CreatePointByCellPosition(in CellPosition cellPosition, BuildBounds bounds)
        {
            var x = bounds.MinX + cellPosition.X * _options.CellSize;
            var y = bounds.MinY + cellPosition.Y * _options.CellSize;
            var z = bounds.MinZ + cellPosition.Z * _options.CellSize;

            return new Point(x, y, z);
        }

        private int GetPointIndex(ISurfaceFunction function,
            BuildBounds bounds,
            in CellPosition position,
            List<Point> vertices,
            ConcurrentDictionary<CellPosition, ConcurrentIndex> indicesMap)
        {
            var concurrentIndex = indicesMap.GetOrAdd(position, new ConcurrentIndex(vertices));
            lock (concurrentIndex)
            {
                if (concurrentIndex.TryGetIndex(out var index))
                {
                    return index;
                }

                var vertex = GetBestVertex(function, position, bounds);
                return concurrentIndex.CreateIndex(vertex);
            }
        }

        private Point GetBestVertex(ISurfaceFunction function, in CellPosition position, BuildBounds bounds)
        {
            var cornerValues = new float[2, 2, 2];
            var cornerPoints = new Point[2, 2, 2];
            foreach (var dx in EnumerationRange)
            {
                foreach (var dy in EnumerationRange)
                {
                    foreach (var dz in EnumerationRange)
                    {
                        cornerPoints[dx, dy, dz] = CreatePointByCellPosition(position.Shift(dx, dy, dz), bounds);
                        cornerValues[dx, dy, dz] = function.GetValue(cornerPoints[dx, dy, dz]);
                    }
                }
            }

            var changes = new List<Point>();
            foreach (var dx in EnumerationRange)
            {
                foreach (var dy in EnumerationRange)
                {
                    var isFirstPointInside = cornerValues[dx, dy, 0] < 0.0f;
                    var isSecondPointInside = cornerValues[dx, dy, 1] < 0.0f;
                    if (isFirstPointInside != isSecondPointInside)
                    {
                        var changePoint = GetChangePoint(function, cornerPoints[dx, dy, 0], cornerValues[dx, dy, 0], cornerPoints[dx, dy, 1], cornerValues[dx, dy, 1], Axis.Z);
                        changes.Add(changePoint);
                    }
                }
            }

            foreach (var dx in EnumerationRange)
            {
                foreach (var dz in EnumerationRange)
                {
                    var isFirstPointInside = cornerValues[dx, 0, dz] < 0.0f;
                    var isSecondPointInside = cornerValues[dx, 1, dz] < 0.0f;
                    if (isFirstPointInside != isSecondPointInside)
                    {
                        var changePoint = GetChangePoint(function, cornerPoints[dx, 0, dz], cornerValues[dx, 0, dz], cornerPoints[dx, 1, dz], cornerValues[dx, 1, dz], Axis.Y);
                        changes.Add(changePoint);
                    }
                }
            }

            foreach (var dy in EnumerationRange)
            {
                foreach (var dz in EnumerationRange)
                {
                    var isFirstPointInside = cornerValues[0, dy, dz] < 0.0f;
                    var isSecondPointInside = cornerValues[1, dy, dz] < 0.0f;
                    if (isFirstPointInside != isSecondPointInside)
                    {
                        var changePoint = GetChangePoint(function, cornerPoints[0, dy, dz], cornerValues[0, dy, dz], cornerPoints[1, dy, dz], cornerValues[1, dy, dz], Axis.X);
                        changes.Add(changePoint);
                    }
                }
            }

            var normals = new List<Vector3>();
            foreach (var changePoint in changes)
            {
                var normal = function.GetNormal(changePoint);
                normals.Add(normal);
            }

            var point = SolveQef(cornerPoints, changes, normals);
            if (!_options.ClipPointToCellBounds)
            {
                return point;
            }

            var min = cornerPoints[0, 0, 0];
            var max = cornerPoints[1, 1, 1];
            if (!IsPointInsideCell(point, min, max))
            {
                return new Point(ClipValue(point.X, min.X, max.X),
                    ClipValue(point.Y, min.Y, max.Y),
                    ClipValue(point.Z, min.Z, max.Z));
            }

            return point;
        }

        private Point GetChangePoint(ISurfaceFunction function,
            in Point firstPoint,
            float firstValue,
            in Point secondPoint,
            float secondValue,
            Axis axis)
        {
            return _options.ChangePointSelectionMode switch
            {
                ChangePointSelectionMode.LinearApproximation => GetChangePointByLinearApproximation(firstPoint, firstValue, secondValue, axis),
                ChangePointSelectionMode.BestPointSelection => GetChangePointByBestPointSelection(function, firstPoint, firstValue, secondPoint, secondValue, axis),
                _ => throw new NotSupportedException("Selected ChangePointSelectionMode is unsupported"),
            };
        }

        private Point GetChangePointByLinearApproximation(in Point firstPoint, float firstValue, float secondValue, Axis axis)
        {
            var delta = -firstValue / (secondValue - firstValue) * _options.CellSize;
            if (axis == Axis.X)
            {
                return new Point(firstPoint.X + delta, firstPoint.Y, firstPoint.Z);
            }

            if (axis == Axis.Y)
            {
                return new Point(firstPoint.X, firstPoint.Y + delta, firstPoint.Z);
            }

            return new Point(firstPoint.X, firstPoint.Y, firstPoint.Z + delta);
        }

        private Point GetChangePointByBestPointSelection(ISurfaceFunction function,
            in Point firstPoint,
            float firstValue,
            in Point secondPoint,
            float secondValue,
            Axis axis)
        {
            var checkPointsCount = GetBestPointChecksCount(firstPoint, secondPoint, axis);
            var checkPoints = PoolPointValue.Shared.Rent(checkPointsCount);

            try
            {
                checkPoints[0] = (firstPoint, firstValue);

                for (int i = 1; i < checkPointsCount - 1; i++)
                {
                    var point = ShiftPointAlongAxis(firstPoint, i, axis);
                    checkPoints[i] = (point, function.GetValue(point));
                }

                checkPoints[checkPointsCount - 1] = (secondPoint, secondValue);

                var isFirstPointInside = checkPoints[0].Value < 0.0f;
                for (int i = 1; i < checkPointsCount; i++)
                {
                    if (checkPoints[i].Value == 0.0f)
                    {
                        return checkPoints[i].Point;
                    }

                    var isPointInside = checkPoints[i].Value < 0.0f;
                    if (isFirstPointInside != isPointInside)
                    {
                        return GetMiddlePoint(checkPoints[i - 1].Point, checkPoints[i].Point, axis);
                    }
                }
            }
            finally
            {
                PoolPointValue.Shared.Return(checkPoints);
            }

            throw new InvalidOperationException("Unable to select change point using best point selection");
        }

        private int GetBestPointChecksCount(in Point firstPoint, in Point secondPoint, Axis axis)
        {
            float delta;
            if (axis == Axis.X)
            {
                delta = secondPoint.X - firstPoint.X;
            }
            else if (axis == Axis.Y)
            {
                delta = secondPoint.Y - firstPoint.Y;
            }
            else
            {
                delta = secondPoint.Z - firstPoint.Z;
            }

            return (int)(delta / _options.BestPointSelectionAccuracy) + 1;
        }

        private Point ShiftPointAlongAxis(in Point firstPoint, int index, Axis axis)
        {
            if (axis == Axis.X)
            {
                return new Point(firstPoint.X + index * _options.BestPointSelectionAccuracy, firstPoint.Y, firstPoint.Z);
            }
            else if (axis == Axis.Y)
            {
                return new Point(firstPoint.X, firstPoint.Y + index * _options.BestPointSelectionAccuracy, firstPoint.Z);
            }
            else
            {
                return new Point(firstPoint.X, firstPoint.Y, firstPoint.Z + index * _options.BestPointSelectionAccuracy);
            }
        }

        private Point SolveQef(Point[,,] cornerPoints, List<Point> changes, List<Vector3> normals)
        {
            var min = cornerPoints[0, 0, 0];
            var max = cornerPoints[1, 1, 1];

            if (_options.UseBias)
            {
                AppendBiases(changes, normals);
            }

            var qef = new QefSolver(changes, normals);
            var point = qef.Solve();

            if (!_options.Boundary || IsPointInsideCell(point, min, max))
            {
                return point;
            }

            var list = PoolPointError.Shared.Rent(6);

            try
            {
                list[0] = qef.FixAxis(Axis.X, min.X).SolveWithError();
                list[1] = qef.FixAxis(Axis.X, max.X).SolveWithError();
                list[2] = qef.FixAxis(Axis.Y, min.Y).SolveWithError();
                list[3] = qef.FixAxis(Axis.Y, max.Y).SolveWithError();
                list[4] = qef.FixAxis(Axis.Z, min.Z).SolveWithError();
                list[5] = qef.FixAxis(Axis.Z, max.Z).SolveWithError();

                if (CheckQefList(list, 6, out point, min, max))
                {
                    return point;
                }
            }
            finally
            {
                PoolPointError.Shared.Return(list);
            }

            list = PoolPointError.Shared.Rent(12);

            try
            {
                list[0] = qef.FixAxis(Axis.Y, min.Y).FixAxis(Axis.X, min.X).SolveWithError();
                list[1] = qef.FixAxis(Axis.Y, max.Y).FixAxis(Axis.X, min.X).SolveWithError();
                list[2] = qef.FixAxis(Axis.Y, min.Y).FixAxis(Axis.X, max.X).SolveWithError();
                list[3] = qef.FixAxis(Axis.Y, max.Y).FixAxis(Axis.X, max.X).SolveWithError();
                list[4] = qef.FixAxis(Axis.Z, min.Z).FixAxis(Axis.X, min.X).SolveWithError();
                list[5] = qef.FixAxis(Axis.Z, max.Z).FixAxis(Axis.X, min.X).SolveWithError();
                list[6] = qef.FixAxis(Axis.Z, min.Z).FixAxis(Axis.X, max.X).SolveWithError();
                list[7] = qef.FixAxis(Axis.Z, max.Z).FixAxis(Axis.X, max.X).SolveWithError();
                list[8] = qef.FixAxis(Axis.Z, min.Z).FixAxis(Axis.Y, min.Y).SolveWithError();
                list[9] = qef.FixAxis(Axis.Z, max.Z).FixAxis(Axis.Y, min.Y).SolveWithError();
                list[10] = qef.FixAxis(Axis.Z, min.Z).FixAxis(Axis.Y, max.Y).SolveWithError();
                list[11] = qef.FixAxis(Axis.Z, max.Z).FixAxis(Axis.Y, max.Y).SolveWithError();

                if (CheckQefList(list, 12, out point, min, max))
                {
                    return point;
                }
            }
            finally
            {
                PoolPointError.Shared.Return(list);
            }

            list = PoolPointError.Shared.Rent(8);

            try
            {
                list[0] = (cornerPoints[0, 0, 0], qef.CalculateErrorInPoint(cornerPoints[0, 0, 0]));
                list[1] = (cornerPoints[0, 0, 1], qef.CalculateErrorInPoint(cornerPoints[0, 0, 1]));
                list[2] = (cornerPoints[0, 1, 0], qef.CalculateErrorInPoint(cornerPoints[0, 1, 0]));
                list[3] = (cornerPoints[0, 1, 1], qef.CalculateErrorInPoint(cornerPoints[0, 1, 1]));
                list[4] = (cornerPoints[1, 0, 0], qef.CalculateErrorInPoint(cornerPoints[1, 0, 0]));
                list[5] = (cornerPoints[1, 0, 1], qef.CalculateErrorInPoint(cornerPoints[1, 0, 1]));
                list[6] = (cornerPoints[1, 1, 0], qef.CalculateErrorInPoint(cornerPoints[1, 1, 0]));
                list[7] = (cornerPoints[1, 1, 1], qef.CalculateErrorInPoint(cornerPoints[1, 1, 1]));

                var minIndex = 0;
                var minError = list[0].Error;
                for (int i = 1; i < 8; i++)
                {
                    var error = list[i].Error;
                    if (error < minError)
                    {
                        minIndex = i;
                        minError = error;
                    }
                }

                return list[minIndex].Point;
            }
            finally
            {
                PoolPointError.Shared.Return(list);
            }
        }

        private void AppendBiases(List<Point> changes, List<Vector3> normals)
        {
            var x = changes[0].X;
            var y = changes[0].Y;
            var z = changes[0].Z;

            for (int i = 1; i < changes.Count; i++)
            {
                x += changes[i].X;
                y += changes[i].Y;
                z += changes[i].Z;
            }

            var massPoint = new Point(x / changes.Count, y / changes.Count, z / changes.Count);
            normals.Add(new Vector3(_options.BiasStrength, 0.0f, 0.0f));
            changes.Add(massPoint);
            normals.Add(new Vector3(0.0f, _options.BiasStrength, 0.0f));
            changes.Add(massPoint);
            normals.Add(new Vector3(0.0f, 0.0f, _options.BiasStrength));
            changes.Add(massPoint);
        }
    }
}
