using ModelBuilder.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;

namespace ModelBuilder.Core.Extensions
{
    public static class MeshExtensions
    {
        public static IEnumerable<Triangle> AsTriangleList(this Mesh mesh)
        {
            return TriangulateMeshIndices(mesh).Select(x => new Triangle
                {
                    FirstPoint = mesh.Vertices[x.Item1],
                    SecondPoint = mesh.Vertices[x.Item2],
                    ThirdPoint = mesh.Vertices[x.Item3],
                    Normal = GetNormalByThreePoints(mesh.Vertices[x.Item1], mesh.Vertices[x.Item2], mesh.Vertices[x.Item3]),
                });
        }

        public static void WriteToObj(this Mesh mesh, string filename, bool includeNormals = false)
        {
            if (includeNormals)
            {
                WriteToObjWithNormals(mesh, filename);
                return;
            }

            WriteToObjWithoutNormals(mesh, filename);
        }

        private static void WriteToObjWithNormals(Mesh mesh, string filename)
        {
            var lines = new List<string>();
            var vertices = mesh.Vertices.Select(x => GetObjVertexLine(x));
            var normals = TriangulateMeshIndices(mesh)
                .Select(x => GetNormalByThreePoints(mesh.Vertices[x.Item1], mesh.Vertices[x.Item2], mesh.Vertices[x.Item3]))
                .Select(x => GetObjNormalLine(x));

            var indices = TriangulateMeshIndices(mesh)
                .Select((x, i) => $"f {x.Item1 + 1}//{i + 1} {x.Item2 + 1}//{i + 1} {x.Item3 + 1}//{i + 1}");

            lines.AddRange(vertices);
            lines.AddRange(normals);
            lines.AddRange(indices);

            File.WriteAllLines(filename, lines);
        }

        private static void WriteToObjWithoutNormals(Mesh mesh, string filename)
        {
            var lines = new List<string>();
            var vertices = mesh.Vertices.Select(x => GetObjVertexLine(x));
            var indices = TriangulateMeshIndices(mesh)
                .Select(x => $"f {x.Item1 + 1} {x.Item2 + 1} {x.Item3 + 1}");

            lines.AddRange(vertices);
            lines.AddRange(indices);

            File.WriteAllLines(filename, lines);
        }

        private static string GetObjVertexLine(in Point point)
        {
            return $"v {point.X.ToString(CultureInfo.InvariantCulture)} " +
                $"{point.Y.ToString(CultureInfo.InvariantCulture)} " +
                $"{point.Z.ToString(CultureInfo.InvariantCulture)}";
        }

        private static string GetObjNormalLine(in Vector3 normal)
        {
            return $"vn {normal.X.ToString(CultureInfo.InvariantCulture)} " +
                $"{normal.Y.ToString(CultureInfo.InvariantCulture)} " +
                $"{normal.Z.ToString(CultureInfo.InvariantCulture)}";
        }

        private static IEnumerable<ValueTuple<int, int, int>> TriangulateMeshIndices(Mesh mesh)
        {
            return mesh.Indices.Select((item, index) => new { item, index })
                .GroupBy(x => x.index / 3)
                .Select(x => x.Select(y => y.item))
                .Select(x => ValueTuple.Create(x.ElementAt(0), x.ElementAt(1), x.ElementAt(2)));
        }

        private static Vector3 GetNormalByThreePoints(in Point first, in Point second, in Point third)
        {
            var firstDirection = new Vector3(second.X - first.X, second.Y - first.Y, second.Z - first.Z);
            var secondDirection = new Vector3(third.X - first.X, third.Y - first.Y, third.Z - first.Z);
            var direction = Vector3.Cross(firstDirection, secondDirection);

            return Vector3.Normalize(direction);
        }
    }
}
