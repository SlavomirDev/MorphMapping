using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MorphMapping
{
    /// <summary>
    /// Default <see cref="IContractResolver"/>. Chooses a specialized <see cref="MappingContract"/>
    /// subclass per type: primitives, enums, <see cref="Nullable{T}"/>, arrays, dictionaries,
    /// generic collections and finally complex objects.
    /// </summary>
    public sealed class DefaultContractResolver : IContractResolver
    {
        private readonly ConcurrentDictionary<Type, MappingContract> _cache =
            new ConcurrentDictionary<Type, MappingContract>();

        public MappingContract ResolveContract(Type type)
        {
            if (type is null) throw new ArgumentNullException(nameof(type));
            return _cache.GetOrAdd(type, BuildContract);
        }

        private static MappingContract BuildContract(Type type)
        {
            // Nullable<T>.
            var underlying = Nullable.GetUnderlyingType(type);
            if (underlying != null)
            {
                return new NullableContract(type, underlying);
            }

            // Enum.
            if (type.IsEnum)
            {
                return new EnumContract(type);
            }

            // Primitives, strings, DateTime, decimal, Guid, TimeSpan, DateTimeOffset.
            if (IsLeafType(type))
            {
                return new PrimitiveContract(type);
            }

            // Arrays.
            if (type.IsArray)
            {
                var elementType = type.GetElementType() ?? typeof(object);
                return new ArrayContract(type, elementType);
            }

            // Dictionaries must be checked before generic enumerables.
            if (TryGetDictionaryArgs(type, out var keyType, out var valueType))
            {
                return new DictionaryContract(type, keyType, valueType);
            }

            // Generic enumerables / collections.
            if (type != typeof(string) && TryGetEnumerableElementType(type, out var elementType2))
            {
                return new EnumerableContract(type, elementType2);
            }

            // Fallback: complex object.
            return BuildObjectContract(type);
        }

        private static ObjectContract BuildObjectContract(Type type)
        {
            var properties = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToArray();

            var constructors = type
                .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(c => c.GetParameters().Length)
                .ToArray();

            return new ObjectContract(type, properties, constructors);
        }

        private static bool IsLeafType(Type type)
        {
            if (type.IsPrimitive) return true;
            if (type == typeof(string)) return true;
            if (type == typeof(decimal)) return true;
            if (type == typeof(DateTime)) return true;
            if (type == typeof(DateTimeOffset)) return true;
            if (type == typeof(TimeSpan)) return true;
            if (type == typeof(Guid)) return true;
            return false;
        }

        private static bool TryGetDictionaryArgs(Type type, out Type keyType, out Type valueType)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            {
                var args = type.GetGenericArguments();
                keyType = args[0];
                valueType = args[1];
                return true;
            }

            foreach (var i in type.GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                {
                    var args = i.GetGenericArguments();
                    keyType = args[0];
                    valueType = args[1];
                    return true;
                }
            }

            keyType = typeof(object);
            valueType = typeof(object);
            return false;
        }

        private static bool TryGetEnumerableElementType(Type type, out Type elementType)
        {
            if (type.IsArray)
            {
                elementType = type.GetElementType() ?? typeof(object);
                return true;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }

            foreach (var i in type.GetInterfaces())
            {
                if (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    elementType = i.GetGenericArguments()[0];
                    return true;
                }
            }

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                elementType = typeof(object);
                return true;
            }

            elementType = typeof(object);
            return false;
        }
    }
}
