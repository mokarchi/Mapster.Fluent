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
            if (serviceCollection == null) throw new ArgumentNullException(nameof(serviceCollection));
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
            configureOptions.Invoke(options);

            // Create and wrap TypeAdapterConfig
            var innerConfig = new TypeAdapterConfig();
            IFluentMapperConfig config = new FluentTypeAdapterConfig(innerConfig);
            options.ConfigureAction?.Invoke(config);

            // Assembly scanning
            if (options.AssembliesToScan?.Any() == true)
            {
                innerConfig.Scan(options.AssembliesToScan.ToArray());
            }

            // Register the configuration
            serviceCollection.TryAddSingleton(config.GetInnerConfig());

            // Register mapper based on UseServiceMapper option
            if (options.UseServiceMapper)
            {
                serviceCollection.TryAddTransient<IMapper, ServiceMapper>();
                serviceCollection.TryAddSingleton<IMapContextFactory, DefaultMapContextFactory>();
            }
            else
            {
                serviceCollection.TryAddTransient<IMapper, Mapper>();
            }

            return serviceCollection;
        }

        /// <summary>
        /// Adds Mapster with fluent configuration using IFluentMapperConfig, enabling chainable mapping rules and DI support with ServiceMapper.
        /// </summary>
        /// <param name="serviceCollection">The service collection to add Mapster services to.</param>
        /// <param name="configure">Action to configure fluent mapping rules using IFluentMapperConfig.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddMapsterFluent(
            this IServiceCollection serviceCollection,
            Action<IFluentMapperConfig> configure)
        {
            if (serviceCollection == null) throw new ArgumentNullException(nameof(serviceCollection));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var innerConfig = new TypeAdapterConfig();
            IFluentMapperConfig config = new FluentTypeAdapterConfig(innerConfig);
            configure.Invoke(config);

            serviceCollection.TryAddSingleton(config.GetInnerConfig());
            serviceCollection.TryAddTransient<IMapper, ServiceMapper>();
            serviceCollection.TryAddSingleton<IMapContextFactory, DefaultMapContextFactory>();

            return serviceCollection;
        }

        /// <summary>
        /// Adds Mapster using an existing TypeAdapterConfig, enabling DI support with ServiceMapper and IMapContextFactory.
        /// </summary>
        /// <param name="serviceCollection">The service collection to add Mapster services to.</param>
        /// <param name="existingConfig">The pre-configured TypeAdapterConfig to use for mapping.</param>
        /// <returns>The service collection for method chaining.</returns>
        public static IServiceCollection AddMapsterWithConfig(
            this IServiceCollection serviceCollection,
            TypeAdapterConfig existingConfig)
        {
            if (serviceCollection == null) throw new ArgumentNullException(nameof(serviceCollection));
            if (existingConfig == null) throw new ArgumentNullException(nameof(existingConfig));

            IFluentMapperConfig config = new FluentTypeAdapterConfig(existingConfig);

            serviceCollection.TryAddSingleton(config.GetInnerConfig());
            serviceCollection.TryAddTransient<IMapper, ServiceMapper>();
            serviceCollection.TryAddSingleton<IMapContextFactory, DefaultMapContextFactory>();

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
