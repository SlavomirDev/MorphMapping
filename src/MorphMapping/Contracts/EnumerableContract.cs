using System;
using System.Collections;
using System.Collections.Generic;

namespace MorphMapping
{
    /// <summary>
    /// Contract for generic enumerable / collection destinations (not dictionaries). Builds either
    /// a <see cref="List{T}"/> or, if the destination type is concrete with a matching <c>Add</c>
    /// method, an instance of that type. Nested element conversions go through
    /// <see cref="MappingContext.Convert"/>.
    /// </summary>
    public sealed class EnumerableContract : MappingContract
    {
        public Type ElementType { get; }

        public EnumerableContract(Type type, Type elementType) : base(type)
        {
            ElementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
        }

        public override object? Map(object? source, object? destination, MappingContext context)
        {
            if (source is null) return null;
            if (source is not IEnumerable enumerable)
            {
                context.HandleError(null, $"Cannot map '{source.GetType().Name}' to enumerable of '{ElementType.Name}': source is not enumerable.");
                return null;
            }

            var listType = typeof(List<>).MakeGenericType(ElementType);
            var list = (IList)Activator.CreateInstance(listType)!;
            foreach (var item in enumerable)
            {
                list.Add(Map(item, ElementType, null, context));
            }

            if (Type.IsAssignableFrom(listType))
            {
                return list;
            }

            if (!Type.IsAbstract && !Type.IsInterface)
            {
                try
                {
                    var instance = context.ObjectFactory.CreateInstance(Type);
                    var addMethod = Type.GetMethod("Add", new[] { ElementType });
                    if (instance != null && addMethod != null)
                    {
                        foreach (var item in list)
                        {
                            addMethod.Invoke(instance, new[] { item });
                        }
                        return instance;
                    }
                }
                catch (Exception ex)
                {
                    context.HandleError(ex, $"Failed to build destination collection '{Type.Name}'.");
                }
            }

            return list;
        }
    }
}
