using System;

namespace MorphMapping
{
    /// <summary>Thrown when mapping fails and <see cref="MapperOptions.ThrowOnError"/> is enabled.</summary>
    public class MappingException : Exception
    {
        public MappingException(string message) : base(message) { }
        public MappingException(string message, Exception innerException) : base(message, innerException) { }
    }
}
