using System;

namespace MorphMapping
{
    /// <summary>
    /// Marks a property to be excluded from mapping.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class MappingIgnoreAttribute : Attribute { }
}
