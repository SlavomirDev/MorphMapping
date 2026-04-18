using System;

namespace MorphMapping
{
    /// <summary>
    /// Main entry point for object-to-object mapping.
    /// </summary>
    public interface IMapper
    {
        /// <summary>
        /// Maps <paramref name="source"/> into a new instance of <typeparamref name="TDestination"/>.
        /// </summary>
        TDestination Map<TDestination>(object source, Action<MappingContext>? configureContext = null);

        /// <summary>
        /// Maps <paramref name="source"/> into the provided existing <paramref name="destination"/> instance.
        /// </summary>
        TDestination Map<TDestination>(object source, TDestination destination, Action<MappingContext>? configureContext = null);

        /// <summary>
        /// Maps <paramref name="source"/> into a new instance of <paramref name="destinationType"/>.
        /// </summary>
        object? Map(object source, Type destinationType, Action<MappingContext>? configureContext = null);
    }
}
