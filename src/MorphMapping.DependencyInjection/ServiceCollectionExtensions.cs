using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MorphMapping.DependencyInjection
{
    /// <summary>
    /// Registration helpers that wire <see cref="IMapper"/> into an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="IMapper"/> as a singleton with default options.
        /// </summary>
        public static IServiceCollection AddMorphMapper(this IServiceCollection services)
        {
            return services.AddMorphMapper(configure: null, configureOptions: null);
        }

        /// <summary>
        /// Registers <see cref="IMapper"/> as a singleton, calling <paramref name="configureOptions"/>
        /// to customize <see cref="MapperOptions"/>.
        /// </summary>
        public static IServiceCollection AddMorphMapper(this IServiceCollection services, Action<MapperOptions> configureOptions)
        {
            if (configureOptions is null) throw new ArgumentNullException(nameof(configureOptions));
            return services.AddMorphMapper(configure: null, configureOptions: configureOptions);
        }

        /// <summary>
        /// Registers <see cref="IMapper"/> as a singleton, exposing a <see cref="MapperBuilder"/>
        /// for fluent configuration.
        /// </summary>
        public static IServiceCollection AddMorphMapper(this IServiceCollection services, Action<MapperBuilder> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            return services.AddMorphMapper(configure: configure, configureOptions: null);
        }

        /// <summary>
        /// Registers <see cref="IMapper"/> as a singleton with both option- and builder-level configuration.
        /// </summary>
        public static IServiceCollection AddMorphMapper(
            this IServiceCollection services,
            Action<MapperBuilder>? configure,
            Action<MapperOptions>? configureOptions)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));

            services.AddSingleton<IMapper>(provider =>
            {
                var options = new MapperOptions();
                configureOptions?.Invoke(options);

                var builder = new MapperBuilder(options);
                configure?.Invoke(builder);

                var loggerFactory = provider.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
                return builder.Build(loggerFactory);
            });

            return services;
        }
    }
}
