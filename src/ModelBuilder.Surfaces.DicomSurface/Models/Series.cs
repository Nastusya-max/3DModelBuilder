using System;

namespace ModelBuilder.Surfaces.DicomSurface.Models
{
    public class Series
    {
        public string Id { get; set; }

        public int Number { get; set; }

        public string Description { get; set; }

        public int ImagesCount { get; set; }

        public DateTime? DateTime { get; set; }
    }
}
