using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;
using System.Reflection;

namespace Mapster.Fluent
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds Mapster with default configuration (uses regular Mapper, no DI support).
        /// Preserved for backward compatibility.
        /// </summary>
        public static IServiceCollection AddMapster(this IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddTransient<IMapper, Mapper>();
            return serviceCollection;
        }

        /// <summary>
        /// Adds Mapster with fluent configuration options.
        /// </summary>
        /// <param name="serviceCollection">The service collection.</param>
        /// <param name="configureOptions">Action to configure MapsterOptions.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddMapster(
            this IServiceCollection serviceCollection,
            Action<MapsterOptions> configureOptions)
        {
            if (serviceCollection == null) throw new ArgumentNullException(nameof(serviceCollection));
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            var options = new MapsterOptions();
            configureOptions?.Invoke(options);

            var config = new TypeAdapterConfig();
            options.ConfigureAction?.Invoke(config);

            // Assembly scanning
            if (options.AssembliesToScan?.Any() == true)
            {
                config.Scan(options.AssembliesToScan.ToArray());
            }

            // Register the configuration
            serviceCollection.TryAddSingleton(config);

            // Register the appropriate mapper
            if (options.UseServiceMapper)
            {
                serviceCollection.TryAddTransient<IMapper, ServiceMapper>();
            }
            else
            {
                serviceCollection.TryAddTransient<IMapper>(sp => new Mapper(config));
            }

            return serviceCollection;
        }

        /// <summary>
        /// Scans assemblies for IRegister and IMapFrom implementations.
        /// </summary>
        /// <param name="serviceCollection">The service collection.</param>
        /// <param name="assemblies">Assemblies to scan.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection ScanMapster(
            this IServiceCollection serviceCollection,
            params Assembly[] assemblies)
        {
            if (serviceCollection == null) throw new ArgumentNullException(nameof(serviceCollection));
            if (assemblies == null || assemblies.Length == 0)
                throw new ArgumentException("At least one assembly must be provided", nameof(assemblies));

            return serviceCollection.AddMapster(options =>
            {
                options.AssembliesToScan = assemblies;
                options.UseServiceMapper = true;
            });
        }
    }
}
