using System;
using System.Reflection;

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
        /// Central conversion dispatcher. Checks class-level <see cref="MappingConverterAttribute"/>
        /// first (destination wins over source), then global converters, and finally resolves and
        /// delegates to the target contract. Called by <see cref="Mapper"/> for the root call and
        /// by contracts for nested / child values.
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

            // Class-level converters declared via [MappingConverter] on the destination or source
            // type. More specific than globals (they are tied to concrete types), so they win
            // over the global converter list below.
            var classConverter = ResolveClassLevelConverter(sourceType, destinationType);
            if (classConverter != null)
            {
                try
                {
                    return classConverter.Convert(source, destination, context);
                }
                catch (Exception ex)
                {
                    context.HandleError(ex, $"Class-level converter '{classConverter.GetType().Name}' threw.");
                    if (context.Options.ThrowOnError) throw;
                    return MappingContext.GetDefault(destinationType);
                }
            }

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

        /// <summary>
        /// Resolves a <see cref="MappingConverter"/> declared via
        /// <see cref="MappingConverterAttribute"/> on either the destination type (preferred,
        /// matching the destination-property-wins convention) or the source type. The returned
        /// converter must be compatible with the actual <paramref name="sourceType"/> and
        /// <paramref name="destinationType"/>; otherwise it is ignored.
        /// </summary>
        private static MappingConverter? ResolveClassLevelConverter(Type sourceType, Type destinationType)
        {
            var destAttr = destinationType.GetCustomAttribute<MappingConverterAttribute>(inherit: true);
            if (destAttr != null)
            {
                var converter = TryCreateConverter(destAttr.ConverterType);
                if (converter != null &&
                    converter.SourceType.IsAssignableFrom(sourceType) &&
                    destinationType.IsAssignableFrom(converter.DestinationType))
                {
                    return converter;
                }
            }

            var srcAttr = sourceType.GetCustomAttribute<MappingConverterAttribute>(inherit: true);
            if (srcAttr != null)
            {
                var converter = TryCreateConverter(srcAttr.ConverterType);
                if (converter != null &&
                    converter.SourceType.IsAssignableFrom(sourceType) &&
                    destinationType.IsAssignableFrom(converter.DestinationType))
                {
                    return converter;
                }
            }

            return null;
        }

        private static MappingConverter? TryCreateConverter(Type converterType)
            => Activator.CreateInstance(converterType) as MappingConverter;
    }
}
