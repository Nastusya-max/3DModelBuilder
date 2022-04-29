using FellowOakDicom;
using FellowOakDicom.Imaging;
using FellowOakDicom.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelBuilder.Surfaces.DicomSurface.Interfaces;
using System;

namespace ModelBuilder.Surfaces.DicomSurface
{
    public static class ServiceCollectionExtensions
    {
        public static void AddDicomSurface(this IServiceCollection services,
            Func<IServiceProvider, DicomDirectory> dicomDirectorySelector,
            IOptions<DicomSurfaceOptions> options)
        {
            services.AddImageManager<ImageSharpImageManager>();
            services.AddScoped<IDicomDirectoryReader, DicomDirectoryReader>(x => new DicomDirectoryReader(dicomDirectorySelector.Invoke(x), options.Value));
        }

        public static void AddDicomSurface(this IServiceCollection services, Func<IServiceProvider, DicomDirectory> dicomDirectorySelector)
        {
            var options = Options.Create(DicomSurfaceOptions.Default);
            services.AddDicomSurface(dicomDirectorySelector, options);
        }
    }
}
