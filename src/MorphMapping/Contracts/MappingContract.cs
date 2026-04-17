using System;

namespace MorphMapping
{
    /// <summary>
    /// Base class for all mapping contracts. A contract owns the reflection and data-transfer
    /// logic for a specific destination <see cref="System.Type"/>. The <see cref="Mapper"/> is a
    /// thin coordinator: it seeds a <see cref="MappingContext"/> and delegates all real work to
    /// the contract returned by <see cref="IContractResolver.ResolveContract"/>.
    /// </summary>
    public abstract class MappingContract
    {
        /// <summary>Destination type this contract governs.</summary>
        public Type Type { get; }

        protected MappingContract(Type type)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
        }

        /// <summary>
        /// Converts <paramref name="source"/> into a value assignable to <see cref="Type"/>.
        /// </summary>
        /// <param name="source">The source value; may be <c>null</c>.</param>
        /// <param name="destination">An existing destination instance to populate; may be <c>null</c> to build a new one.</param>
        /// <param name="context">Mapping context (user state + resolver, factory, options, diagnostics).</param>
        public abstract object? Map(object? source, object? destination, MappingContext context);

        /// <summary>
        /// Central conversion dispatcher. Checks global converters first, then resolves and
        /// delegates to the target contract. Called by <see cref="Mapper"/> for the root call
        /// and by contracts for nested / child values.
        /// </summary>
        protected internal static object? Map(
            object? source,
            Type destinationType,
            object? destination,
            MappingContext context)
        {
            if (destinationType is null) throw new ArgumentNullException(nameof(destinationType));
            if (context is null) throw new ArgumentNullException(nameof(context));

            if (source is null)
            {
                return destination ?? MappingContext.GetDefault(destinationType);
            }

            var sourceType = source.GetType();

            // Global converters (ordered, first match wins). Attribute-level converters on
            // individual properties are the concern of the property-copying contract — they
            // are resolved there and never reach this path.
            foreach (var converter in context.Options.Converters)
            {
                if (converter.SourceType.IsAssignableFrom(sourceType) &&
                    destinationType.IsAssignableFrom(converter.DestinationType))
                {
                    try
                    {
                        return converter.Convert(source, destination, context);
                    }
                    catch (Exception ex)
                    {
                        context.HandleError(ex, $"Converter '{converter.GetType().Name}' threw.");
                        if (context.Options.ThrowOnError) throw;
                        return MappingContext.GetDefault(destinationType);
                    }
                }
            }

            var contract = context.Resolver.ResolveContract(destinationType);
            return contract.Map(source, destination, context);
        }
    }
}
