using ModelBuilder.Surfaces.DicomSurface.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ModelBuilder.Surfaces.DicomSurface.Interfaces
{
    public interface IDicomDirectoryReader
    {
        List<Patient> GetAllPatients();

        Task<DicomSurface> GetSurfaceAsync(string seriesId);
    }
}
