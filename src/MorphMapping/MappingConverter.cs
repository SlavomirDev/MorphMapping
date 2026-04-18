using System;

namespace MorphMapping
{
    /// <summary>
    /// Non-generic base for custom converters. Prefer the generic <see cref="MappingConverter{TSource,TDestination}"/>.
    /// </summary>
    public abstract class MappingConverter
    {
        public abstract Type SourceType { get; }
        public abstract Type DestinationType { get; }
        public abstract object? Convert(object? source, object? destination, MappingContext context);
    }

    /// <summary>
    /// Strongly-typed converter for a specific source→destination pair. Converters can be registered
    /// globally through <see cref="MapperOptions.Converters"/> or attached to individual properties via
    /// <see cref="MappingConverterAttribute"/>.
    /// </summary>
    public abstract class MappingConverter<TSource, TDestination> : MappingConverter
    {
        public override Type SourceType => typeof(TSource);
        public override Type DestinationType => typeof(TDestination);

        public abstract TDestination? Convert(TSource? source, TDestination? destination, MappingContext context);

        public override object? Convert(object? source, object? destination, MappingContext context)
        {
            var src = source is TSource typedSrc ? typedSrc : default;
            var dst = destination is TDestination typedDst ? typedDst : default;
            return Convert(src, dst, context);
        }
    }

    /// <summary>
    /// Declares a converter to be applied to a specific property or class.
    /// <para>
    /// When placed on a property, the converter is invoked for that property during copy
    /// (source value → destination property).
    /// </para>
    /// <para>
    /// When placed on a class, the converter is invoked whenever the mapping pipeline targets
    /// that class — either as a root destination type or as a nested value — provided the
    /// source/destination types are compatible with the converter. A converter declared on
    /// the destination type wins over one declared on the source type, and both win over any
    /// matching global converter registered in <see cref="MapperOptions.Converters"/>.
    /// </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class MappingConverterAttribute : Attribute
    {
        public Type ConverterType { get; }

        public MappingConverterAttribute(Type converterType)
        {
            if (converterType is null) throw new ArgumentNullException(nameof(converterType));
            if (!typeof(MappingConverter).IsAssignableFrom(converterType))
            {
                throw new ArgumentException(
                    $"Converter type must inherit from {nameof(MappingConverter)}.",
                    nameof(converterType));
            }
            ConverterType = converterType;
        }
    }
}
