using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MorphMapping
{
    /// <summary>
    /// Default <see cref="IMapper"/> implementation. The mapper owns no reflection: it seeds a
    /// <see cref="MappingContext"/> with the configured options, resolver, factory and logger,
    /// then hands control to <see cref="MappingContract.Map"/> which dispatches through
    /// converters and contracts. All real mapping lives in the <see cref="MappingContract"/>
    /// hierarchy. Instances are built through <see cref="MapperBuilder"/> (or the companion
    /// <c>MorphMapping.DependencyInjection</c> package).
    /// </summary>
    public class Mapper : IMapper
    {
        private readonly MapperOptions _options;
        private readonly ILogger _logger;

        internal Mapper(MapperOptions options, ILogger<Mapper>? logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? NullLogger<Mapper>.Instance;
        }

        public TDestination Map<TDestination>(object source, Action<MappingContext>? configureContext = null)
            where TDestination : class
        {
            if (source is null) throw new ArgumentNullException(nameof(source));

            var context = CreateContext(configureContext);
            return (TDestination)MappingContract.Map(source, typeof(TDestination), null, context)!;
        }

        public TDestination Map<TDestination>(object source, TDestination destination, Action<MappingContext>? configureContext = null)
            where TDestination : class
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (destination is null) throw new ArgumentNullException(nameof(destination));

            var context = CreateContext(configureContext);
            return (TDestination)MappingContract.Map(source, typeof(TDestination), destination, context)!;
        }

        public object? Map(object source, Type destinationType, Action<MappingContext>? configureContext = null)
        {
            if (source is null) throw new ArgumentNullException(nameof(source));
            if (destinationType is null) throw new ArgumentNullException(nameof(destinationType));

            var context = CreateContext(configureContext);
            return MappingContract.Map(source, destinationType, null, context);
        }

        private MappingContext CreateContext(Action<MappingContext>? configureContext)
        {
            var context = new MappingContext(
                _options,
                _options.ContractResolver,
                _options.ObjectFactory,
                _logger,
                this);
            configureContext?.Invoke(context);
            return context;
        }
    }
}
