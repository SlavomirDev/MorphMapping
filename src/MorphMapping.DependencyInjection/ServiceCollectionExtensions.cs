using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using System;

namespace MorphMapping.DependencyInjection
{
    /// <summary>
    /// Registration helpers that wire <see cref="IMapper"/> into an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="IMapper"/> as a singleton and returns a <see cref="MapperBuilder"/>
        /// for further fluent configuration. The builder's queued configuration is applied lazily
        /// when the mapper is first resolved from the container, so any <c>.Configure&lt;...&gt;()</c>
        /// chained after this call will still be picked up.
        /// </summary>
        /// <param name="services">The service collection to register the mapper into.</param>
        /// <returns>A <see cref="MapperBuilder"/> used to configure the mapper fluently.</returns>
        public static MapperBuilder AddMorphMapper(this IServiceCollection services)
            => services.AddMorphMapper(configure: null);

        /// <summary>
        /// Registers <see cref="IMapper"/> as a singleton with an options-level configuration action
        /// and returns a <see cref="MapperBuilder"/> for further fluent configuration.
        /// <para>
        /// The <paramref name="configure"/> callback is invoked first (before any builder actions)
        /// at mapper-resolution time, so it always sees a fresh <see cref="MapperOptions"/>.
        /// </para>
        /// </summary>
        /// <param name="services">The service collection to register the mapper into.</param>
        /// <param name="configure">Optional action used to configure <see cref="MapperOptions"/>.</param>
        /// <returns>A <see cref="MapperBuilder"/> used to configure the mapper fluently.</returns>
        public static MapperBuilder AddMorphMapper(this IServiceCollection services, Action<MapperOptions>? configure)
        {
            _ = services ?? throw new ArgumentNullException(nameof(services));

            var builder = new MapperBuilder();

            // Queue the options-level configuration first so it runs before any per-pair builder
            // actions when the mapper is materialized from DI.
            if (configure is not null)
                builder.ConfigureOptions(configure);

            services.TryAddSingleton<IMapper>(provider =>
            {
                var loggerFactory = provider.GetService<ILoggerFactory>();
                return builder.Build(loggerFactory);
            });

            return builder;
        }
    }
}
