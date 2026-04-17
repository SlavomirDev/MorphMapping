using System;

namespace MorphMapping
{
    internal sealed class MapperConfigurator<TSource, TDestination> : IMapperConfigurator<TSource, TDestination>
    {
        private readonly PairConfiguration _config;

        public MapperConfigurator(PairConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public IMapperConfigurator<TSource, TDestination> MapProperty(string sourceProperty, string destinationProperty)
        {
            if (string.IsNullOrEmpty(sourceProperty)) throw new ArgumentNullException(nameof(sourceProperty));
            if (string.IsNullOrEmpty(destinationProperty)) throw new ArgumentNullException(nameof(destinationProperty));

            _config.PropertyMappings[sourceProperty] = destinationProperty;
            return this;
        }

        public IMapperConfigurator<TSource, TDestination> MapProperty(string destinationProperty, Func<TSource, object?> valueProvider)
        {
            if (string.IsNullOrEmpty(destinationProperty)) throw new ArgumentNullException(nameof(destinationProperty));
            if (valueProvider is null) throw new ArgumentNullException(nameof(valueProvider));

            _config.ValueProviders[destinationProperty] = valueProvider;
            return this;
        }

        public IMapperConfigurator<TSource, TDestination> IgnoreProperty(string destinationProperty)
        {
            if (string.IsNullOrEmpty(destinationProperty)) throw new ArgumentNullException(nameof(destinationProperty));
            _config.IgnoredProperties.Add(destinationProperty);
            return this;
        }

        public IMapperConfigurator<TSource, TDestination> BeforeMapping(Action<TSource, TDestination> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));
            _config.BeforeMappings.Add(action);
            return this;
        }

        public IMapperConfigurator<TSource, TDestination> BeforeMapping(Action<TSource, TDestination, MappingContext> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));
            _config.BeforeMappings.Add(action);
            return this;
        }

        public IMapperConfigurator<TSource, TDestination> AfterMapping(Action<TSource, TDestination> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));
            _config.AfterMappings.Add(action);
            return this;
        }

        public IMapperConfigurator<TSource, TDestination> AfterMapping(Action<TSource, TDestination, MappingContext> action)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));
            _config.AfterMappings.Add(action);
            return this;
        }

        public IMapperConfigurator<TSource, TDestination> CustomMapping(Action<TSource, TDestination> mapping)
        {
            if (mapping is null) throw new ArgumentNullException(nameof(mapping));
            _config.CustomMapping = mapping;
            _config.CustomMappingHasContext = false;
            return this;
        }

        public IMapperConfigurator<TSource, TDestination> CustomMapping(Action<TSource, TDestination, MappingContext> mapping)
        {
            if (mapping is null) throw new ArgumentNullException(nameof(mapping));
            _config.CustomMapping = mapping;
            _config.CustomMappingHasContext = true;
            return this;
        }
    }
}
