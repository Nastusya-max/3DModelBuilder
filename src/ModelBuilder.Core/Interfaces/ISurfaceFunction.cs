using ModelBuilder.Core.Models;
using System.Numerics;

namespace ModelBuilder.Core.Interfaces
{
    public interface ISurfaceFunction
    {
        float GetValue(in Point point);

        Vector3 GetNormal(in Point point);
    }
}
