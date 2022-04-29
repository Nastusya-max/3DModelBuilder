using System;
using System.Collections.Generic;

namespace ModelBuilder.Surfaces.DicomSurface.Models
{
    public class Study
    {
        public string Id { get; set; }

        public string Description { get; set; }

        public DateTime? DateTime { get; set; }

        public List<Series> Series { get; set; }
    }
}
