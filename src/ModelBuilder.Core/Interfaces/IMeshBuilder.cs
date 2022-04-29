using ModelBuilder.Core.Models;

namespace ModelBuilder.Core.Interfaces
{
    public interface IMeshBuilder
    {
        Mesh Build(ISurfaceFunction function, BuildBounds bounds);
    }
}
