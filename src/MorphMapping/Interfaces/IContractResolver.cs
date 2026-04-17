using System;

namespace MorphMapping
{
    /// <summary>
    /// Resolves how a given <see cref="Type"/> should be mapped. The returned
    /// <see cref="MappingContract"/> owns the reflection and data-transfer logic for the type.
    /// </summary>
    public interface IContractResolver
    {
        /// <summary>Returns the contract that governs mapping to/from <paramref name="type"/>.</summary>
        MappingContract ResolveContract(Type type);
    }
}
