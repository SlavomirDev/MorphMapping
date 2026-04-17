using System;
using System.Collections;
using System.Collections.Generic;

namespace MorphMapping
{
    /// <summary>
    /// Contract for array destinations (<c>T[]</c>). Iterates any <see cref="IEnumerable"/> source,
    /// converts each element through the context and builds a correctly-sized array.
    /// </summary>
    public sealed class ArrayContract : MappingContract
    {
        public Type ElementType { get; }

        public ArrayContract(Type type, Type elementType) : base(type)
        {
            ElementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
        }

        public override object? Map(object? source, object? destination, MappingContext context)
        {
            if (source is null) return null;
            if (source is not IEnumerable enumerable)
            {
                context.HandleError(null, $"Cannot map '{source.GetType().Name}' to array of '{ElementType.Name}': source is not enumerable.");
                return null;
            }

            var buffer = new List<object?>();
            foreach (var item in enumerable)
            {
                buffer.Add(Map(item, ElementType, null, context));
            }

            var array = Array.CreateInstance(ElementType, buffer.Count);
            for (int i = 0; i < buffer.Count; i++)
            {
                array.SetValue(buffer[i], i);
            }
            return array;
        }
    }
}
