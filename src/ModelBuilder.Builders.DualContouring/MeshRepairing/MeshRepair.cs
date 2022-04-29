using System.Runtime.InteropServices;

namespace ModelBuilder.Builders.DualContouring.MeshRepairing
{
    internal static class MeshRepair
    {
        public static void RepairMesh(Vertex[] vertices, int[] indices)
        {
            RepairMeshInternal(vertices, vertices.Length, indices, indices.Length);
        }

        [DllImport("D:\\source\\repos\\ModelBuilder\\src\\ModelBuilder.MeshRepair\\x64\\Debug\\ModelBuilder.MeshRepair.dll", EntryPoint = "RepairMesh", CallingConvention = CallingConvention.Cdecl)]
        private static extern void RepairMeshInternal([In] Vertex[] vertices, int verticesCount, int[] indices, int indicesCount);
    }
}
