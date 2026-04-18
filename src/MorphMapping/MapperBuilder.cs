using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;

namespace MorphMapping
{
    /// <summary>
    /// Fluent builder for <see cref="IMapper"/>. Standalone: can be used directly without DI, or
    /// driven by the companion <c>MorphMapping.DependencyInjection</c> package.
    /// </summary>
    public sealed class MapperBuilder
    {
        private readonly MapperOptions? _boundOptions;
        private readonly List<Action<MapperOptions>> _configurationActions = new();

        /// <summary>Creates a new builder that will build a fresh <see cref="MapperOptions"/> on <see cref="Build(ILoggerFactory?)"/>.</summary>
        public MapperBuilder() { }

        /// <summary>
        /// Creates a builder bound to an externally-owned <see cref="MapperOptions"/>.
        /// Queued actions are applied to this instance when <see cref="Build(ILoggerFactory?)"/> is called.
        /// </summary>
        /// <param name="options">Externally-owned options instance to mutate on build.</param>
        public MapperBuilder(MapperOptions options)
        {
            _boundOptions = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Queues a raw mutation of <see cref="MapperOptions"/> (top-level flags, converters, resolver, etc.).
        /// </summary>
        /// <param name="configure">Action that mutates <see cref="MapperOptions"/>.</param>
        /// <returns>The same builder for fluent chaining.</returns>
        public MapperBuilder ConfigureOptions(Action<MapperOptions> configure)
        {
            _ = configure ?? throw new ArgumentNullException(nameof(configure));
            _configurationActions.Add(configure);
            return this;
        }

        /// <summary>
        /// Queues a per-pair configuration for the <typeparamref name="TSource"/> →
        /// <typeparamref name="TDestination"/> mapping.
        /// </summary>
        /// <typeparam name="TSource">Source type.</typeparam>
        /// <typeparam name="TDestination">Destination type.</typeparam>
        /// <param name="configure">Callback that receives the per-pair <see cref="IMapperConfigurator{TSource, TDestination}"/>.</param>
        /// <returns>The same builder for fluent chaining.</returns>
        public MapperBuilder Configure<TSource, TDestination>(Action<IMapperConfigurator<TSource, TDestination>> configure)
        {
            _ = configure ?? throw new ArgumentNullException(nameof(configure));

            _configurationActions.Add(options =>
            {
                var key = (typeof(TSource), typeof(TDestination));
                if (!options.PairConfigurations.TryGetValue(key, out var config))
                {
                    config = new PairConfiguration();
                    options.PairConfigurations[key] = config;
                }

                configure(new MapperConfigurator<TSource, TDestination>(config));
            });

            return this;
        }

        /// <summary>Queues a global before-mapping hook (runs for every pair).</summary>
        /// <param name="action">Action invoked with the source and destination (untyped).</param>
        /// <returns>The same builder for fluent chaining.</returns>
        public MapperBuilder GlobalBeforeMapping(Action<object, object> action)
        {
            _ = action ?? throw new ArgumentNullException(nameof(action));
            _configurationActions.Add(options => options.GlobalBeforeMappings.Add((s, d, _) => action(s, d)));
            return this;
        }

        /// <summary>Queues a global before-mapping hook with access to the <see cref="MappingContext"/>.</summary>
        /// <param name="action">Action invoked with the source, destination and mapping context.</param>
        /// <returns>The same builder for fluent chaining.</returns>
        public MapperBuilder GlobalBeforeMapping(Action<object, object, MappingContext> action)
        {
            _ = action ?? throw new ArgumentNullException(nameof(action));
            _configurationActions.Add(options => options.GlobalBeforeMappings.Add((s, d, c) => action(s, d, c)));
            return this;
        }

        /// <summary>Queues a global after-mapping hook (runs for every pair).</summary>
        /// <param name="action">Action invoked with the source and destination (untyped).</param>
        /// <returns>The same builder for fluent chaining.</returns>
        public MapperBuilder GlobalAfterMapping(Action<object, object> action)
        {
            _ = action ?? throw new ArgumentNullException(nameof(action));
            _configurationActions.Add(options => options.GlobalAfterMappings.Add((s, d, _) => action(s, d)));
            return this;
        }

        /// <summary>Queues a global after-mapping hook with access to the <see cref="MappingContext"/>.</summary>
        /// <param name="action">Action invoked with the source, destination and mapping context.</param>
        /// <returns>The same builder for fluent chaining.</returns>
        public MapperBuilder GlobalAfterMapping(Action<object, object, MappingContext> action)
        {
            _ = action ?? throw new ArgumentNullException(nameof(action));
            _configurationActions.Add(options => options.GlobalAfterMappings.Add((s, d, c) => action(s, d, c)));
            return this;
        }

        /// <summary>
        /// Replays every queued configuration action against <paramref name="options"/>.
        /// Used by the DI extension when the mapper is being materialized from the container.
        /// </summary>
        /// <param name="options">Target options to mutate.</param>
        internal void ApplyTo(MapperOptions options)
        {
            _ = options ?? throw new ArgumentNullException(nameof(options));
            foreach (var action in _configurationActions)
                action(options);
        }

        /// <summary>
        /// Builds a standalone <see cref="IMapper"/> instance by replaying the queued configuration
        /// against a new <see cref="MapperOptions"/> (or the options passed to
        /// <see cref="MapperBuilder(MapperOptions)"/>). The optional <paramref name="loggerFactory"/>
        /// enables internal error logging; when omitted, errors are silently ignored (unless
        /// <see cref="MapperOptions.ThrowOnError"/> is set).
        /// </summary>
        /// <param name="loggerFactory">Optional logger factory used to build the mapper's logger.</param>
        /// <returns>A fully-configured <see cref="IMapper"/> instance.</returns>
        public IMapper Build(ILoggerFactory? loggerFactory = null)
        {
            var options = _boundOptions ?? new MapperOptions();
            ApplyTo(options);

            var logger = loggerFactory?.CreateLogger<Mapper>();
            return new Mapper(options, logger);
        }
    }
}
