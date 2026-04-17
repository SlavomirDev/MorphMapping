using System;

namespace MorphMapping
{
    /// <summary>
    /// Contract for <see cref="Nullable{T}"/> destinations. Delegates to the context to convert
    /// into the underlying type.
    /// </summary>
    public sealed class NullableContract : MappingContract
    {
        /// <summary>The underlying <c>T</c> of <see cref="Nullable{T}"/>.</summary>
        public Type UnderlyingType { get; }

        public NullableContract(Type type, Type underlyingType) : base(type)
        {
            UnderlyingType = underlyingType ?? throw new ArgumentNullException(nameof(underlyingType));
        }

        public override object? Map(object? source, object? destination, MappingContext context)
        {
            if (source is null) return null;
            return Map(source, UnderlyingType, null, context);
        }
    }
}
