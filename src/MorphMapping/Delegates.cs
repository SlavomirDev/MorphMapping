using System;

namespace MorphMapping
{
    /// <summary>Delegate for <c>Before</c>/<c>After</c> hooks without context access.</summary>
    public delegate void MappingAction<TSource, TDestination>(TSource source, TDestination destination);

    /// <summary>Delegate for <c>Before</c>/<c>After</c> hooks with context access.</summary>
    public delegate void MappingContextAction<TSource, TDestination>(TSource source, TDestination destination, MappingContext context);

    /// <summary>Delegate for global hooks without strong typing.</summary>
    public delegate void GlobalMappingAction(object source, object destination);

    /// <summary>Delegate for global hooks with context access.</summary>
    public delegate void GlobalMappingContextAction(object source, object destination, MappingContext context);

    /// <summary>Delegate to compute a destination value from a source instance.</summary>
    public delegate object? ValueProvider<TSource>(TSource source);
}
