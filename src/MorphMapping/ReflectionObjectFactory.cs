using System;
using System.Reflection;

namespace MorphMapping
{
    /// <summary>
    /// Default <see cref="IObjectFactory"/>. Uses <see cref="Activator.CreateInstance(Type)"/>
    /// for parameterless construction and <see cref="ConstructorInfo.Invoke(object[])"/>
    /// for constructors with arguments.
    /// </summary>
    public sealed class ReflectionObjectFactory : IObjectFactory
    {
        /// <summary>Shared reusable singleton (stateless).</summary>
        public static readonly ReflectionObjectFactory Instance = new ReflectionObjectFactory();

        public object? CreateInstance(Type type)
        {
            if (type is null) throw new ArgumentNullException(nameof(type));
            return Activator.CreateInstance(type);
        }

        public object? CreateInstance(Type type, ConstructorInfo constructor, object?[] arguments)
        {
            if (type is null) throw new ArgumentNullException(nameof(type));
            if (constructor is null) throw new ArgumentNullException(nameof(constructor));
            if (arguments is null) throw new ArgumentNullException(nameof(arguments));
            return constructor.Invoke(arguments);
        }
    }
}
