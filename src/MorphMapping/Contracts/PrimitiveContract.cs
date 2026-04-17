using System;
using System.Globalization;

namespace MorphMapping
{
    /// <summary>
    /// Contract for leaf types: primitives, <see cref="string"/>, <see cref="decimal"/>,
    /// <see cref="DateTime"/>, <see cref="DateTimeOffset"/>, <see cref="TimeSpan"/> and
    /// <see cref="Guid"/>. Applies identity mapping when types match and
    /// <see cref="System.Convert.ChangeType(object, Type)"/>-based conversion otherwise.
    /// </summary>
    public sealed class PrimitiveContract : MappingContract
    {
        public PrimitiveContract(Type type) : base(type) { }

        public override object? Map(object? source, object? destination, MappingContext context)
        {
            if (source is null) return GetDefault(Type);

            var sourceType = source.GetType();

            // Identity fast-path.
            if (Type.IsAssignableFrom(sourceType))
            {
                return source;
            }

            // Guid: support parsing from string.
            if (Type == typeof(Guid))
            {
                if (source is string guidStr && Guid.TryParse(guidStr, out var guid))
                {
                    return guid;
                }
                context.HandleError(null, $"Cannot convert '{sourceType.Name}' to Guid.");
                return GetDefault(Type);
            }

            // Enum source → numeric destination.
            if (sourceType.IsEnum && IsNumeric(Type))
            {
                try
                {
                    return System.Convert.ChangeType(source, Type, CultureInfo.InvariantCulture);
                }
                catch (Exception ex)
                {
                    context.HandleError(ex, $"Failed to convert enum {sourceType.Name} to {Type.Name}.");
                    if (context.Options.ThrowOnError) throw;
                    return GetDefault(Type);
                }
            }

            try
            {
                return System.Convert.ChangeType(source, Type, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                context.HandleError(ex, $"Failed to convert '{sourceType.Name}' to '{Type.Name}'.");
                if (context.Options.ThrowOnError) throw;
                return GetDefault(Type);
            }
        }

        private static object? GetDefault(Type type)
        {
            if (type.IsValueType) return Activator.CreateInstance(type);
            return null;
        }

        private static bool IsNumeric(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
    }
}
