using System;

namespace MorphMapping
{
    /// <summary>
    /// Fluent per-pair configuration surface for source → destination mapping rules.
    /// </summary>
    public interface IMapperConfigurator<TSource, TDestination>
    {
        /// <summary>Maps a source property to a destination property by name.</summary>
        IMapperConfigurator<TSource, TDestination> MapProperty(string sourceProperty, string destinationProperty);

        /// <summary>Maps a destination property using a custom value provider.</summary>
        IMapperConfigurator<TSource, TDestination> MapProperty(string destinationProperty, Func<TSource, object?> valueProvider);

        /// <summary>Excludes a destination property from automatic mapping.</summary>
        IMapperConfigurator<TSource, TDestination> IgnoreProperty(string destinationProperty);

        /// <summary>Hook invoked before auto-mapping for this pair.</summary>
        IMapperConfigurator<TSource, TDestination> BeforeMapping(Action<TSource, TDestination> action);

        /// <summary>Hook invoked before auto-mapping for this pair, with access to <see cref="MappingContext"/>.</summary>
        IMapperConfigurator<TSource, TDestination> BeforeMapping(Action<TSource, TDestination, MappingContext> action);

        /// <summary>Hook invoked after auto-mapping for this pair.</summary>
        IMapperConfigurator<TSource, TDestination> AfterMapping(Action<TSource, TDestination> action);

        /// <summary>Hook invoked after auto-mapping for this pair, with access to <see cref="MappingContext"/>.</summary>
        IMapperConfigurator<TSource, TDestination> AfterMapping(Action<TSource, TDestination, MappingContext> action);

        /// <summary>
        /// Completely replaces the default copy pipeline with a user-supplied one.
        /// </summary>
        IMapperConfigurator<TSource, TDestination> CustomMapping(Action<TSource, TDestination> mapping);

        /// <summary>
        /// Completely replaces the default copy pipeline with a user-supplied one, with context access.
        /// </summary>
        IMapperConfigurator<TSource, TDestination> CustomMapping(Action<TSource, TDestination, MappingContext> mapping);
    }
}
