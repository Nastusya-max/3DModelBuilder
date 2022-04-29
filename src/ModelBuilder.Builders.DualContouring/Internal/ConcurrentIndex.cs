using ModelBuilder.Core.Models;
using System.Collections.Generic;

namespace ModelBuilder.Builders.DualContouring.Internal
{
    internal class ConcurrentIndex
    {
        private readonly List<Point> _vertices;
        private int? _index;

        public ConcurrentIndex(List<Point> vertices)
        {
            _vertices = vertices;
        }

        public bool TryGetIndex(out int index)
        {
            if (_index.HasValue)
            {
                index = _index.Value;
                return true;
            }

            index = default;
            return false;
        }

        public int CreateIndex(Point point)
        {
            lock (_vertices)
            {
                _index = _vertices.Count;
                _vertices.Add(point);
            }

            return _index.Value;
        }
    }
}
