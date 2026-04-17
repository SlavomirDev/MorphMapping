using System;
using Microsoft.Extensions.Logging;

namespace MorphMapping
{
    /// <summary>
    /// Fluent builder for <see cref="IMapper"/>. Standalone: can be used directly without DI, or
    /// driven by the companion <c>MorphMapping.DependencyInjection</c> package.
    /// </summary>
    public sealed class MapperBuilder
    {
        private readonly MapperOptions _options;

        /// <summary>Creates a new builder with a fresh <see cref="MapperOptions"/>.</summary>
        public MapperBuilder() : this(new MapperOptions()) { }

        /// <summary>Creates a builder that mutates the provided <see cref="MapperOptions"/>.</summary>
        public MapperBuilder(MapperOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>Underlying options (exposed for advanced scenarios).</summary>
        public MapperOptions Options => _options;

        /// <summary>Configures global mapper options.</summary>
        public MapperBuilder ConfigureOptions(Action<MapperOptions> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));
            configure(_options);
            return this;
        }

        /// <summary>Configures a per-pair mapping.</summary>
        public MapperBuilder Configure<TSource, TDestination>(Action<IMapperConfigurator<TSource, TDestination>> configure)
        {
            if (configure is null) throw new ArgumentNullException(nameof(configure));

            var key = (typeof(TSource), typeof(TDestination));
            if (!_options.PairConfigurations.TryGetValue(key, out var config))
            {
                config = new PairConfiguration();
                _options.PairConfigurations[key] = config;
            }

            configure(new MapperConfigurator<TSource, TDestination>(config));
            return this;
        }

        /// <summary>Adds a global before-mapping hook (runs for every pair).</summary>
        public MapperBuilder GlobalBeforeMapping(Action<object, object> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));
            _options.GlobalBeforeMappings.Add((s, d, _) => action(s, d));
            return this;
        }

        /// <summary>Adds a global before-mapping hook with context access.</summary>
        public MapperBuilder GlobalBeforeMapping(Action<object, object, MappingContext> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));
            _options.GlobalBeforeMappings.Add((s, d, c) => action(s, d, c));
            return this;
        }

        /// <summary>Adds a global after-mapping hook (runs for every pair).</summary>
        public MapperBuilder GlobalAfterMapping(Action<object, object> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));
            _options.GlobalAfterMappings.Add((s, d, _) => action(s, d));
            return this;
        }

        /// <summary>Adds a global after-mapping hook with context access.</summary>
        public MapperBuilder GlobalAfterMapping(Action<object, object, MappingContext> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));
            _options.GlobalAfterMappings.Add((s, d, c) => action(s, d, c));
            return this;
        }

        /// <summary>
        /// Builds a standalone <see cref="IMapper"/> instance. An optional <paramref name="loggerFactory"/>
        /// enables internal error logging; when omitted, errors are silently ignored (unless
        /// <see cref="MapperOptions.ThrowOnError"/> is set).
        /// </summary>
        public IMapper Build(ILoggerFactory? loggerFactory = null)
        {
            var logger = loggerFactory?.CreateLogger<Mapper>();
            return new Mapper(_options, logger);
        }
    }
}
