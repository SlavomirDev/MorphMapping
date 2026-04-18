using System;
using System.Collections;
using System.Collections.Generic;

namespace MorphMapping
{
    /// <summary>
    /// Contract for dictionary destinations (anything implementing <see cref="IDictionary{TKey,TValue}"/>).
    /// </summary>
    public sealed class DictionaryContract : MappingContract
    {
        public Type KeyType { get; }
        public Type ValueType { get; }

        public DictionaryContract(Type type, Type keyType, Type valueType) : base(type)
        {
            KeyType = keyType ?? throw new ArgumentNullException(nameof(keyType));
            ValueType = valueType ?? throw new ArgumentNullException(nameof(valueType));
        }

        public override object? Map(object? source, object? destination, MappingContext context)
        {
            if (source is null) return null;
            if (source is not IEnumerable enumerable)
            {
                context.HandleError(null, $"Cannot map '{source.GetType().Name}' to dictionary: source is not enumerable.");
                return null;
            }

            Type concreteType;
            if (Type.IsAbstract || Type.IsInterface)
            {
                concreteType = typeof(Dictionary<,>).MakeGenericType(KeyType, ValueType);
            }
            else
            {
                concreteType = Type;
            }

            object? instance;
            try
            {
                instance = context.ObjectFactory.CreateInstance(concreteType);
            }
            catch (Exception ex)
            {
                context.HandleError(ex, $"Failed to create '{concreteType.Name}'.");
                if (context.Options.ThrowOnError) throw;
                return null;
            }
            if (instance is null) return null;

            var addMethod = concreteType.GetMethod("Add", new[] { KeyType, ValueType });
            if (addMethod is null) return instance;

            foreach (var entry in enumerable)
            {
                if (entry is null) continue;
                var entryType = entry.GetType();
                var keyProp = entryType.GetProperty("Key");
                var valueProp = entryType.GetProperty("Value");
                if (keyProp is null || valueProp is null) continue;

                var key = Map(keyProp.GetValue(entry), KeyType, null, context);
                var value = Map(valueProp.GetValue(entry), ValueType, null, context);
                try
                {
                    addMethod.Invoke(instance, new[] { key, value });
                }
                catch (Exception ex)
                {
                    context.HandleError(ex, $"Failed to add entry to dictionary '{concreteType.Name}'.");
                    if (context.Options.ThrowOnError) throw;
                }
            }

            return instance;
        }
    }
}
