using System.Collections.Generic;

namespace ModelBuilder.Surfaces.DicomSurface.Models
{
    public class Patient
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public PatientSex Sex { get; set; }

        public List<Study> Studies { get; set; }
    }
}
