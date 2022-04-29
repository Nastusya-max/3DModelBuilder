using ModelBuilder.Core.Interfaces;
using ModelBuilder.Core.Models;
using ModelBuilder.Surfaces.DicomSurface.Internal;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ModelBuilder.Surfaces.DicomSurface
{
    public class DicomSurface : ISurfaceFunction
    {
        private readonly DicomSurfaceOptions _options;
        private readonly List<DicomLayer> _layers;

        internal DicomSurface(IEnumerable<DicomLayer> layers, DicomSurfaceOptions options)
        {
            _options = options;

            var halfHeight = layers.Sum(x => x.Thickness) / 2.0f;
            var width = layers.Max(x => x.Width);
            var depth = layers.Max(x => x.Height);
            var halfWidth = width / 2.0f;
            var halfDepth = depth / 2.0f;

            SurfaceBounds = new BuildBounds
            {
                MinX = -halfWidth,
                MaxX = halfWidth,
                MinY = -halfHeight,
                MaxY = halfHeight,
                MinZ = -halfDepth,
                MaxZ = halfDepth,
            };

            _layers = layers.OrderBy(x => x.Offset).ToList();
            _layers.ForEach(x => x.Initialize(width, depth, _options));
        }

        public BuildBounds SurfaceBounds { get; }

        public float GetValue(in Point point)
        {
            var index = GetLayerIndexByVerticalCoordinate(point.Y);
            if (index < 0)
            {
                return _options.ActivationThreshold;
            }

            return GetValueFromLayer(_layers[index], point.X, point.Z);
        }

        private int GetLayerIndexByVerticalCoordinate(float y)
        {
            if (y > SurfaceBounds.MaxY || y < SurfaceBounds.MinY)
            {
                return -1;
            }

            var delta = y - SurfaceBounds.MinY;
            var probeHeight = _layers[0].Offset + delta;

            return _layers.FindIndex(x => x.Offset <= probeHeight && x.Offset + x.Thickness >= probeHeight);
        }

        private float GetValueFromLayer(DicomLayer layer, float x, float z)
        {
            if (x > SurfaceBounds.MaxX || x < SurfaceBounds.MinX ||
                z > SurfaceBounds.MaxZ || z < SurfaceBounds.MinZ)
            {
                return _options.ActivationThreshold;
            }

            var deltaX = x - SurfaceBounds.MinX;
            var deltaZ = z - SurfaceBounds.MinZ;

            return layer.GetValue(deltaX, deltaZ);
        }

        public static List<KeyValuePair<Point, Vector3>> normals = new();

        private void LogNormal(in Point point, in Vector3 normal)
        {
            lock (normals)
            {
                normals.Add(new KeyValuePair<Point, Vector3>(point, normal));
            }
        }

        public Vector3 GetNormal(in Point point)
        {
            var trinmPoint = TrimPointToBounds(point);
            var currentLayerIndex = GetLayerIndexByVerticalCoordinate(point.Y);

            var normal = GetXZNormal(trinmPoint, currentLayerIndex) +
                GetXYNormal(trinmPoint, currentLayerIndex) +
                GetYZNormal(trinmPoint, currentLayerIndex) +
                GetFirstXAxisDiagonalNormal(trinmPoint, currentLayerIndex) +
                GetSecondXAxisDiagonalNormal(trinmPoint, currentLayerIndex) +
                GetFirstYAxisDiagonalNormal(trinmPoint, currentLayerIndex) +
                GetSecondYAxisDiagonalNormal(trinmPoint, currentLayerIndex) +
                GetFirstZAxisDiagonalNormal(trinmPoint, currentLayerIndex) +
                GetSecondZAxisDiagonalNormal(trinmPoint, currentLayerIndex);

            normal = normal == Vector3.Zero
                ? Vector3.Zero
                : Vector3.Normalize(normal);

            LogNormal(point, normal);
            return normal;
        }

        private static Vector3 GetNormalFromMatrix(bool[,] matrix, Vector3 forwardDirection, Vector3 rightDirection)
        {
            var ceterX = matrix.GetLength(1) / 2;
            var centerY = matrix.GetLength(0) / 2;

            var isCenterInside = matrix[ceterX, centerY];

            var isForwardInside = matrix[ceterX - 1, centerY];
            var isBackwardInside = matrix[ceterX + 1, centerY];
            var isLeftInside = matrix[ceterX, centerY - 1];
            var isRightInside = matrix[ceterX, centerY + 1];

            var isForwardLeftInside = matrix[ceterX - 1, centerY - 1];
            var isForwardRightInside = matrix[ceterX - 1, centerY + 1];
            var isBackwardLeftInside = matrix[ceterX + 1, centerY - 1];
            var isBackwardRightInside = matrix[ceterX + 1, centerY + 1];

            var forwardMultiplier = GetAxisMultiplier(isCenterInside, isForwardInside);
            var backwardMultiplier = GetAxisMultiplier(isCenterInside, isBackwardInside);
            var leftMultiplier = GetAxisMultiplier(isCenterInside, isLeftInside);
            var rightMultiplier = GetAxisMultiplier(isCenterInside, isRightInside);

            var forwardLeftMultiplier = GetAxisMultiplier(isCenterInside, isForwardLeftInside);
            var forwardRightMultiplier = GetAxisMultiplier(isCenterInside, isForwardRightInside);
            var backwardLeftMultiplier = GetAxisMultiplier(isCenterInside, isBackwardLeftInside);
            var backwardRightMultiplier = GetAxisMultiplier(isCenterInside, isBackwardRightInside);

            var normal = forwardDirection * forwardMultiplier +
                -forwardDirection * backwardMultiplier +
                -rightDirection * leftMultiplier +
                rightDirection * rightMultiplier +
                Vector3.Normalize(forwardDirection + -rightDirection) * forwardLeftMultiplier +
                Vector3.Normalize(forwardDirection + rightDirection) * forwardRightMultiplier +
                Vector3.Normalize(-forwardDirection + -rightDirection) * backwardLeftMultiplier +
                Vector3.Normalize(-forwardDirection + rightDirection) * backwardRightMultiplier;

            return normal;
        }

        private static int GetAxisMultiplier(bool isCenterInside, bool isPointInside)
        {
            if (!isPointInside)
            {
                return isCenterInside ? 1 : 0;
            }

            return isCenterInside ? 0 : -1;
        }

        private Point TrimPointToBounds(in Point point)
        {
            var flag = false;
            var x = point.X;
            if (point.X > SurfaceBounds.MaxX)
            {
                x = SurfaceBounds.MaxX;
                flag = true;
            }
            else if (point.X < SurfaceBounds.MinX)
            {
                x = SurfaceBounds.MinX;
                flag = true;
            }

            var y = point.Y;
            if (point.Y > SurfaceBounds.MaxY)
            {
                y = SurfaceBounds.MaxY;
                flag = true;
            }
            else if (point.Y < SurfaceBounds.MinY)
            {
                y = SurfaceBounds.MinY;
                flag = true;
            }

            var z = point.Z;
            if (point.Z > SurfaceBounds.MaxZ)
            {
                z = SurfaceBounds.MaxZ;
                flag = true;
            }
            else if (point.Y < SurfaceBounds.MinY)
            {
                z = SurfaceBounds.MinZ;
                flag = true;
            }

            if (flag)
            {
                return new Point(x, y, z);
            }

            return point;
        }

        private Vector3 GetXZNormal(in Point point, int centerPointLayerIndex)
        {
            var delta = _options.NormalSelectionAccuracyInsideLayer;
            var layer = _layers[centerPointLayerIndex];

            var matrix = new bool[3, 3];
            matrix[0, 0] = ProbePoint(layer, point.X - delta, point.Z - delta);
            matrix[0, 1] = ProbePoint(layer, point.X, point.Z - delta);
            matrix[0, 2] = ProbePoint(layer, point.X + delta, point.Z - delta);

            matrix[1, 0] = ProbePoint(layer, point.X - delta, point.Z);
            matrix[1, 1] = ProbePoint(layer, point.X, point.Z);
            matrix[1, 2] = ProbePoint(layer, point.X + delta, point.Z);

            matrix[2, 0] = ProbePoint(layer, point.X - delta, point.Z + delta);
            matrix[2, 1] = ProbePoint(layer, point.X, point.Z + delta);
            matrix[2, 2] = ProbePoint(layer, point.X + delta, point.Z + delta);

            return GetNormalFromMatrix(matrix, -Vector3.UnitZ, Vector3.UnitX);
        }

        private Vector3 GetXYNormal(in Point point, int centerPointLayerIndex)
        {
            var delta = _options.NormalSelectionAccuracyInsideLayer;
            var centerLayer = _layers[centerPointLayerIndex];
            var upperLayer = _layers.ElementAtOrDefault(centerPointLayerIndex + 1);
            var lowerLayer = _layers.ElementAtOrDefault(centerPointLayerIndex - 1);

            var matrix = new bool[3, 3];
            matrix[0, 0] = ProbePoint(upperLayer, point.X - delta, point.Z);
            matrix[0, 1] = ProbePoint(upperLayer, point.X, point.Z);
            matrix[0, 2] = ProbePoint(upperLayer, point.X + delta, point.Z);

            matrix[1, 0] = ProbePoint(centerLayer, point.X - delta, point.Z);
            matrix[1, 1] = ProbePoint(centerLayer, point.X, point.Z);
            matrix[1, 2] = ProbePoint(centerLayer, point.X + delta, point.Z);

            matrix[2, 0] = ProbePoint(lowerLayer, point.X - delta, point.Z);
            matrix[2, 1] = ProbePoint(lowerLayer, point.X, point.Z);
            matrix[2, 2] = ProbePoint(lowerLayer, point.X + delta, point.Z);

            return GetNormalFromMatrix(matrix, Vector3.UnitY, Vector3.UnitX);
        }

        private Vector3 GetYZNormal(in Point point, int centerPointLayerIndex)
        {
            var delta = _options.NormalSelectionAccuracyInsideLayer;
            var centerLayer = _layers[centerPointLayerIndex];
            var upperLayer = _layers.ElementAtOrDefault(centerPointLayerIndex + 1);
            var lowerLayer = _layers.ElementAtOrDefault(centerPointLayerIndex - 1);

            var matrix = new bool[3, 3];
            matrix[0, 0] = ProbePoint(upperLayer, point.X, point.Z + delta);
            matrix[0, 1] = ProbePoint(upperLayer, point.X, point.Z);
            matrix[0, 2] = ProbePoint(upperLayer, point.X, point.Z - delta);

            matrix[1, 0] = ProbePoint(centerLayer, point.X, point.Z + delta);
            matrix[1, 1] = ProbePoint(centerLayer, point.X, point.Z);
            matrix[1, 2] = ProbePoint(centerLayer, point.X, point.Z - delta);

            matrix[2, 0] = ProbePoint(lowerLayer, point.X, point.Z + delta);
            matrix[2, 1] = ProbePoint(lowerLayer, point.X, point.Z);
            matrix[2, 2] = ProbePoint(lowerLayer, point.X, point.Z - delta);

            return GetNormalFromMatrix(matrix, Vector3.UnitY, -Vector3.UnitZ);
        }

        private Vector3 GetFirstXAxisDiagonalNormal(in Point point, int centerPointLayerIndex)
        {
            var delta = _options.NormalSelectionAccuracyInsideLayer;
            var centerLayer = _layers[centerPointLayerIndex];
            var upperLayer = _layers.ElementAtOrDefault(centerPointLayerIndex + 1);
            var lowerLayer = _layers.ElementAtOrDefault(centerPointLayerIndex - 1);

            var matrix = new bool[3, 3];
            matrix[0, 0] = ProbePoint(upperLayer, point.X - delta, point.Z - delta);
            matrix[0, 1] = ProbePoint(upperLayer, point.X, point.Z - delta);
            matrix[0, 2] = ProbePoint(upperLayer, point.X + delta, point.Z - delta);

            matrix[1, 0] = ProbePoint(centerLayer, point.X - delta, point.Z);
            matrix[1, 1] = ProbePoint(centerLayer, point.X, point.Z);
            matrix[1, 2] = ProbePoint(centerLayer, point.X + delta, point.Z);

            matrix[2, 0] = ProbePoint(lowerLayer, point.X - delta, point.Z + delta);
            matrix[2, 1] = ProbePoint(lowerLayer, point.X, point.Z + delta);
            matrix[2, 2] = ProbePoint(lowerLayer, point.X + delta, point.Z + delta);

            return GetNormalFromMatrix(matrix, Vector3.Normalize(Vector3.UnitY + -Vector3.UnitZ), Vector3.UnitX);
        }

        private Vector3 GetSecondXAxisDiagonalNormal(in Point point, int centerPointLayerIndex)
        {
            var delta = _options.NormalSelectionAccuracyInsideLayer;
            var centerLayer = _layers[centerPointLayerIndex];
            var upperLayer = _layers.ElementAtOrDefault(centerPointLayerIndex + 1);
            var lowerLayer = _layers.ElementAtOrDefault(centerPointLayerIndex - 1);

            var matrix = new bool[3, 3];
            matrix[0, 0] = ProbePoint(upperLayer, point.X - delta, point.Z + delta);
            matrix[0, 1] = ProbePoint(upperLayer, point.X, point.Z - delta);
            matrix[0, 2] = ProbePoint(upperLayer, point.X + delta, point.Z + delta);

            matrix[1, 0] = ProbePoint(centerLayer, point.X - delta, point.Z);
            matrix[1, 1] = ProbePoint(centerLayer, point.X, point.Z);
            matrix[1, 2] = ProbePoint(centerLayer, point.X + delta, point.Z);

            matrix[2, 0] = ProbePoint(lowerLayer, point.X - delta, point.Z - delta);
            matrix[2, 1] = ProbePoint(lowerLayer, point.X, point.Z + delta);
            matrix[2, 2] = ProbePoint(lowerLayer, point.X + delta, point.Z - delta);

            return GetNormalFromMatrix(matrix, Vector3.Normalize(Vector3.UnitY + Vector3.UnitZ), Vector3.UnitX);
        }

        private Vector3 GetFirstYAxisDiagonalNormal(in Point point, int centerPointLayerIndex)
        {
            var delta = _options.NormalSelectionAccuracyInsideLayer;
            var centerLayer = _layers[centerPointLayerIndex];
            var upperLayer = _layers.ElementAtOrDefault(centerPointLayerIndex + 1);
            var lowerLayer = _layers.ElementAtOrDefault(centerPointLayerIndex - 1);

            var matrix = new bool[3, 3];
            matrix[0, 0] = ProbePoint(upperLayer, point.X - delta, point.Z - delta);
            matrix[0, 1] = ProbePoint(upperLayer, point.X, point.Z);
            matrix[0, 2] = ProbePoint(upperLayer, point.X + delta, point.Z + delta);

            matrix[1, 0] = ProbePoint(centerLayer, point.X - delta, point.Z - delta);
            matrix[1, 1] = ProbePoint(centerLayer, point.X, point.Z);
            matrix[1, 2] = ProbePoint(centerLayer, point.X + delta, point.Z + delta);

            matrix[2, 0] = ProbePoint(lowerLayer, point.X - delta, point.Z - delta);
            matrix[2, 1] = ProbePoint(lowerLayer, point.X, point.Z);
            matrix[2, 2] = ProbePoint(lowerLayer, point.X + delta, point.Z + delta);

            return GetNormalFromMatrix(matrix, Vector3.UnitY, Vector3.Normalize(Vector3.UnitX + Vector3.UnitZ));
        }

        private Vector3 GetSecondYAxisDiagonalNormal(in Point point, int centerPointLayerIndex)
        {
            var delta = _options.NormalSelectionAccuracyInsideLayer;
            var centerLayer = _layers[centerPointLayerIndex];
            var upperLayer = _layers.ElementAtOrDefault(centerPointLayerIndex + 1);
            var lowerLayer = _layers.ElementAtOrDefault(centerPointLayerIndex - 1);

            var matrix = new bool[3, 3];
            matrix[0, 0] = ProbePoint(upperLayer, point.X - delta, point.Z + delta);
            matrix[0, 1] = ProbePoint(upperLayer, point.X, point.Z);
            matrix[0, 2] = ProbePoint(upperLayer, point.X + delta, point.Z - delta);

            matrix[1, 0] = ProbePoint(centerLayer, point.X - delta, point.Z + delta);
            matrix[1, 1] = ProbePoint(centerLayer, point.X, point.Z);
            matrix[1, 2] = ProbePoint(centerLayer, point.X + delta, point.Z - delta);

            matrix[2, 0] = ProbePoint(lowerLayer, point.X - delta, point.Z + delta);
            matrix[2, 1] = ProbePoint(lowerLayer, point.X, point.Z);
            matrix[2, 2] = ProbePoint(lowerLayer, point.X + delta, point.Z - delta);

            return GetNormalFromMatrix(matrix, Vector3.UnitY, Vector3.Normalize(Vector3.UnitX - Vector3.UnitZ));
        }

        private Vector3 GetFirstZAxisDiagonalNormal(in Point point, int centerPointLayerIndex)
        {
            var delta = _options.NormalSelectionAccuracyInsideLayer;
            var centerLayer = _layers[centerPointLayerIndex];
            var upperLayer = _layers.ElementAtOrDefault(centerPointLayerIndex + 1);
            var lowerLayer = _layers.ElementAtOrDefault(centerPointLayerIndex - 1);

            var matrix = new bool[3, 3];
            matrix[0, 0] = ProbePoint(upperLayer, point.X + delta, point.Z - delta);
            matrix[0, 1] = ProbePoint(upperLayer, point.X + delta, point.Z);
            matrix[0, 2] = ProbePoint(upperLayer, point.X + delta, point.Z + delta);

            matrix[1, 0] = ProbePoint(centerLayer, point.X, point.Z - delta);
            matrix[1, 1] = ProbePoint(centerLayer, point.X, point.Z);
            matrix[1, 2] = ProbePoint(centerLayer, point.X, point.Z + delta);

            matrix[2, 0] = ProbePoint(lowerLayer, point.X - delta, point.Z - delta);
            matrix[2, 1] = ProbePoint(lowerLayer, point.X - delta, point.Z);
            matrix[2, 2] = ProbePoint(lowerLayer, point.X - delta, point.Z + delta);

            return GetNormalFromMatrix(matrix, Vector3.Normalize(Vector3.UnitX + Vector3.UnitY), Vector3.UnitZ);
        }

        private Vector3 GetSecondZAxisDiagonalNormal(in Point point, int centerPointLayerIndex)
        {
            var delta = _options.NormalSelectionAccuracyInsideLayer;
            var centerLayer = _layers[centerPointLayerIndex];
            var upperLayer = _layers.ElementAtOrDefault(centerPointLayerIndex + 1);
            var lowerLayer = _layers.ElementAtOrDefault(centerPointLayerIndex - 1);

            var matrix = new bool[3, 3];
            matrix[0, 0] = ProbePoint(upperLayer, point.X - delta, point.Z + delta);
            matrix[0, 1] = ProbePoint(upperLayer, point.X - delta, point.Z);
            matrix[0, 2] = ProbePoint(upperLayer, point.X - delta, point.Z - delta);

            matrix[1, 0] = ProbePoint(centerLayer, point.X, point.Z + delta);
            matrix[1, 1] = ProbePoint(centerLayer, point.X, point.Z);
            matrix[1, 2] = ProbePoint(centerLayer, point.X, point.Z - delta);

            matrix[2, 0] = ProbePoint(lowerLayer, point.X + delta, point.Z + delta);
            matrix[2, 1] = ProbePoint(lowerLayer, point.X + delta, point.Z);
            matrix[2, 2] = ProbePoint(lowerLayer, point.X + delta, point.Z - delta);

            return GetNormalFromMatrix(matrix, Vector3.Normalize(-Vector3.UnitX + Vector3.UnitY), -Vector3.UnitZ);
        }

        private bool ProbePoint(DicomLayer layer, float x, float z)
        {
            return layer != null && GetValueFromLayer(layer, x, z) <= 0.0f;
        }
    }
}
