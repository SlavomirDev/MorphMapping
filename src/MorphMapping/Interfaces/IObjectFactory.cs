using System;
using System.Reflection;

namespace MorphMapping
{
    /// <summary>
    /// Creates destination instances during mapping. The default implementation
    /// (<see cref="ReflectionObjectFactory"/>) uses reflection — swap it in
    /// <see cref="MapperOptions.ObjectFactory"/> to eliminate reflection cost
    /// (pre-compiled delegates, source generators, DI-resolved instances, …).
    /// </summary>
    public interface IObjectFactory
    {
        /// <summary>Creates an instance using a parameterless constructor or equivalent mechanism.</summary>
        object? CreateInstance(Type type);

        /// <summary>Creates an instance by invoking the specified constructor with the given arguments.</summary>
        object? CreateInstance(Type type, ConstructorInfo constructor, object?[] arguments);
    }
}
