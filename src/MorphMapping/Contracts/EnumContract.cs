using System;
using System.Globalization;

namespace MorphMapping
{
    /// <summary>
    /// Contract for <see cref="Enum"/> destinations. Parses from strings and converts from any
    /// numeric or enum source via <see cref="Enum.ToObject(Type, object)"/>.
    /// </summary>
    public sealed class EnumContract : MappingContract
    {
        public EnumContract(Type type) : base(type)
        {
            if (!type.IsEnum)
                throw new ArgumentException($"Type '{type.Name}' is not an enum.", nameof(type));
        }

        public override object? Map(object? source, object? destination, MappingContext context)
        {
            if (source is null)
            {
                return Activator.CreateInstance(Type);
            }

            if (source is string s)
            {
                try
                {
                    return Enum.Parse(Type, s, ignoreCase: true);
                }
                catch (Exception ex)
                {
                    context.HandleError(ex, $"Cannot parse '{s}' as enum {Type.Name}.");
                    if (context.Options.ThrowOnError) throw;
                    return Activator.CreateInstance(Type);
                }
            }

            var sourceType = source.GetType();

            if (sourceType.IsEnum)
            {
                try
                {
                    var underlying = Enum.GetUnderlyingType(Type);
                    var numeric = System.Convert.ChangeType(source, underlying, CultureInfo.InvariantCulture);
                    return Enum.ToObject(Type, numeric);
                }
                catch (Exception ex)
                {
                    context.HandleError(ex, $"Cannot convert enum '{sourceType.Name}' to '{Type.Name}'.");
                    if (context.Options.ThrowOnError) throw;
                    return Activator.CreateInstance(Type);
                }
            }

            if (IsNumeric(sourceType))
            {
                try
                {
                    return Enum.ToObject(Type, source);
                }
                catch (Exception ex)
                {
                    context.HandleError(ex, $"Cannot convert numeric to enum '{Type.Name}'.");
                    if (context.Options.ThrowOnError) throw;
                    return Activator.CreateInstance(Type);
                }
            }

            context.HandleError(null, $"Cannot convert '{sourceType.Name}' to enum '{Type.Name}'.");
            return Activator.CreateInstance(Type);
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
                    return true;
                default:
                    return false;
            }
        }
    }
}
