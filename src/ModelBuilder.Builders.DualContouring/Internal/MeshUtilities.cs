using g3;
using gs;
using ModelBuilder.Builders.DualContouring.MeshRepairing;
using ModelBuilder.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace ModelBuilder.Builders.DualContouring.Internal
{
    internal class MeshUtilities
    {
        private readonly DMesh3 _mesh;

        public MeshUtilities(List<Point> vertices, List<int> indices)
        {
            var g3Vertices = vertices.Select(x => new Vector3d(x.X, x.Y, x.Z));
            var g3Indices = indices.Select((item, index) => new { item, index })
                .GroupBy(x => x.index / 3)
                .Select(x => x.Select(y => y.item))
                .Select(x => new Index3i(x.ElementAt(0), x.ElementAt(1), x.ElementAt(2)));

            _mesh = DMesh3Builder.Build<Vector3d, Index3i, Vector3f>(g3Vertices, g3Indices);
            MeshNormals.QuickCompute(_mesh);
        }

        public Mesh Mesh => GetMesh();

        public void RepairMesh()
        {
            AutoFix(5, MeshAutoRepair.RemoveModes.Interior);
            RemoveUnconnectedParts();
            AutoFix(5, MeshAutoRepair.RemoveModes.None);

            //var vertices = _mesh.Vertices().Select(x => new Vertex((float)x.x, (float)x.y, (float)x.z)).ToArray();
            //var indices = _mesh.Triangles().SelectMany(x => x.array).ToArray();

            //MeshRepair.RepairMesh(vertices, indices);
        }

        private void AutoFix(int iterations, MeshAutoRepair.RemoveModes removeMode)
        {
            for (int i = 0; i < iterations; i++)
            {
                var auto = new MeshAutoRepair(_mesh);
                auto.RemoveMode = removeMode;
                auto.Apply();

                _mesh.Copy(auto.Mesh);
            }
        }

        private void RemoveUnconnectedParts()
        {
            var assembly = new MeshAssembly(_mesh)
            {
                HasNoVoids = true,
            };

            assembly.Decompose();
            var mesh = assembly.ClosedSolids.Union(assembly.OpenMeshes).OrderByDescending(x => x.TriangleCount).First();
            _mesh.Copy(mesh);
        }

        private Mesh GetMesh()
        {
            var vertices = _mesh.Vertices().Select(x => new Point((float)x.x, (float)x.y, (float)x.z)).ToList();
            var indices = _mesh.Triangles().SelectMany(x => x.array).ToList();

            return new Mesh
            {
                Indices = indices,
                Vertices = vertices,
            };
        }
    }
}
