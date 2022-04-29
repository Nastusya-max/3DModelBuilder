using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ModelBuilder.Core.Interfaces;

namespace ModelBuilder.Builders.DualContouring
{
    public static class ServiceCollectionExtensions
    {
        public static void AddDualContouringBuilder(this IServiceCollection services)
        {
            var options = Options.Create(DualContouringBuilderOptions.Default);
            services.AddDualContouringBuilder(options);
        }

        public static void AddDualContouringBuilder(this IServiceCollection services, IOptions<DualContouringBuilderOptions> options)
        {
            services.AddSingleton<IMeshBuilder, DualContouringBuilder>(x => new DualContouringBuilder(options.Value));
        }
    }
}
