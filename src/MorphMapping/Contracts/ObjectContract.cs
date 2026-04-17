using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MorphMapping
{
    /// <summary>
    /// Contract for complex objects: POCOs, DTOs, entities. Owns the full reflection pipeline —
    /// constructor resolution, per-pair configuration lookup, property copy, attribute-level
    /// converter invocation and global/per-pair before/after hooks. Nested values are always
    /// delegated back through <see cref="MappingContract.Map"/>; instance creation goes
    /// through <see cref="MappingContext.ObjectFactory"/>.
    /// </summary>
    public class ObjectContract : MappingContract
    {
        /// <summary>All public readable properties (sorted by declaration order).</summary>
        public IReadOnlyList<PropertyInfo> Properties { get; }

        /// <summary>All public instance constructors, widest first.</summary>
        public IReadOnlyList<ConstructorInfo> Constructors { get; }

        public ObjectContract(Type type, IReadOnlyList<PropertyInfo> properties, IReadOnlyList<ConstructorInfo> constructors)
            : base(type)
        {
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
            Constructors = constructors ?? throw new ArgumentNullException(nameof(constructors));
        }

        public override object? Map(object? source, object? destination, MappingContext context)
        {
            if (source is null) return destination;

            var sourceType = source.GetType();

            // Resolve source contract as well so we can reuse its property metadata.
            var sourceContract = context.Resolver.ResolveContract(sourceType) as ObjectContract;

            var pairConfig = context.GetPairConfiguration(sourceType, Type);

            // Destination instance first — hooks need a non-null destination.
            if (destination is null)
            {
                destination = CreateInstance(source, sourceContract, pairConfig, context);
                if (destination is null) return null;
            }

            // Global before.
            context.RunGlobalBefore(source, destination);

            // Pair before.
            if (pairConfig != null)
            {
                RunPairHooks(pairConfig.BeforeMappings, source, destination, context);
            }

            // Copy phase: custom mapping fully replaces the default property-copy flow.
            if (pairConfig?.CustomMapping != null)
            {
                InvokeCustomMapping(pairConfig, source, destination, context, sourceType);
            }
            else
            {
                CopyProperties(source, sourceContract, destination, pairConfig, context);
            }

            // Pair after.
            if (pairConfig != null)
            {
                RunPairHooks(pairConfig.AfterMappings, source, destination, context);
            }

            // Global after.
            context.RunGlobalAfter(source, destination);

            return destination;
        }

        /// <summary>
        /// Builds a fresh destination instance. Tries the widest matching constructor first —
        /// each of its parameters must be resolvable from the source (via value providers,
        /// remapped property names, or name-matched source properties). Falls back to a
        /// parameterless constructor through <see cref="MappingContext.ObjectFactory"/>.
        /// </summary>
        internal virtual object? CreateInstance(
            object source,
            ObjectContract? sourceContract,
            PairConfiguration? pairConfig,
            MappingContext context)
        {
            foreach (var ctor in Constructors)
            {
                var parameters = ctor.GetParameters();
                if (parameters.Length == 0) continue;

                var args = new object?[parameters.Length];
                var resolved = true;

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (!TryResolveCtorArgument(source, sourceContract, pairConfig, parameters[i], context, out args[i]))
                    {
                        resolved = false;
                        break;
                    }
                }

                if (resolved)
                {
                    try
                    {
                        return context.ObjectFactory.CreateInstance(Type, ctor, args);
                    }
                    catch (Exception ex)
                    {
                        context.HandleError(ex, $"Constructor {Type.Name}(...) threw during mapping.");
                    }
                }
            }

            if (context.Options.FallbackToParameterlessConstructor)
            {
                // Any parameterless ctor counts — the factory knows how to use it.
                var parameterless = Constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
                if (parameterless != null)
                {
                    try
                    {
                        return context.ObjectFactory.CreateInstance(Type, parameterless, Array.Empty<object?>());
                    }
                    catch (Exception ex)
                    {
                        context.HandleError(ex, $"Parameterless constructor of {Type.Name} threw.");
                    }
                }
            }

            try
            {
                return context.ObjectFactory.CreateInstance(Type);
            }
            catch (Exception ex)
            {
                context.HandleError(ex, $"Failed to create instance of {Type.Name}.");
                if (context.Options.ThrowOnError) throw;
                return null;
            }
        }

        private static bool TryResolveCtorArgument(
            object source,
            ObjectContract? sourceContract,
            PairConfiguration? pairConfig,
            ParameterInfo parameter,
            MappingContext context,
            out object? value)
        {
            // Per-pair value provider keyed by parameter name.
            if (pairConfig != null &&
                pairConfig.ValueProviders.TryGetValue(parameter.Name ?? string.Empty, out var provider))
            {
                try
                {
                    var raw = provider.DynamicInvoke(source);
                    value = Map(raw, parameter.ParameterType, null, context);
                    return true;
                }
                catch (Exception ex)
                {
                    context.HandleError(ex, $"ValueProvider for constructor parameter '{parameter.Name}' threw.");
                    if (context.Options.ThrowOnError) throw;
                    value = null;
                    return false;
                }
            }

            // Remapped source property name (if the user declared a rename targeting this parameter name).
            var sourceName = parameter.Name ?? string.Empty;
            if (pairConfig != null)
            {
                var hit = pairConfig.PropertyMappings.FirstOrDefault(kv =>
                    string.Equals(kv.Value, parameter.Name, StringComparison.OrdinalIgnoreCase));
                if (!hit.Equals(default(KeyValuePair<string, string>)))
                {
                    sourceName = hit.Key;
                }
            }

            var srcProp = FindSourceProperty(sourceContract, source, sourceName);
            if (srcProp != null)
            {
                object? raw;
                try
                {
                    raw = srcProp.GetValue(source);
                }
                catch (Exception ex)
                {
                    context.HandleError(ex, $"Reading source property '{srcProp.Name}' threw.");
                    if (context.Options.ThrowOnError) throw;
                    value = null;
                    return false;
                }

                value = Map(raw, parameter.ParameterType, null, context);
                return true;
            }

            if (parameter.HasDefaultValue)
            {
                value = parameter.DefaultValue;
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>Copies properties from <paramref name="source"/> into <paramref name="destination"/>.</summary>
        internal virtual void CopyProperties(
            object source,
            ObjectContract? sourceContract,
            object destination,
            PairConfiguration? pairConfig,
            MappingContext context)
        {
            foreach (var destProp in Properties)
            {
                if (!destProp.CanWrite) continue;
                if (destProp.GetCustomAttribute<MappingIgnoreAttribute>() != null) continue;
                if (pairConfig != null && pairConfig.IgnoredProperties.Contains(destProp.Name)) continue;

                // Per-pair value provider short-circuits everything else.
                if (pairConfig != null && pairConfig.ValueProviders.TryGetValue(destProp.Name, out var provider))
                {
                    try
                    {
                        var raw = provider.DynamicInvoke(source);
                        destProp.SetValue(destination, ConvertPropertyValue(raw, destProp, context));
                    }
                    catch (Exception ex)
                    {
                        context.HandleError(ex, $"ValueProvider for '{destProp.Name}' threw.");
                        if (context.Options.ThrowOnError) throw;
                    }
                    continue;
                }

                // Resolve source property.
                var sourceName = destProp.Name;
                if (pairConfig != null)
                {
                    var hit = pairConfig.PropertyMappings.FirstOrDefault(kv =>
                        string.Equals(kv.Value, destProp.Name, StringComparison.OrdinalIgnoreCase));
                    if (!hit.Equals(default(KeyValuePair<string, string>)))
                    {
                        sourceName = hit.Key;
                    }
                }

                var srcProp = FindSourceProperty(sourceContract, source, sourceName);
                if (srcProp is null) continue;

                object? srcValue;
                try
                {
                    srcValue = srcProp.GetValue(source);
                }
                catch (Exception ex)
                {
                    context.HandleError(ex, $"Reading source property '{srcProp.Name}' threw.");
                    if (context.Options.ThrowOnError) throw;
                    continue;
                }

                try
                {
                    destProp.SetValue(destination, ConvertPropertyValue(srcValue, destProp, context));
                }
                catch (Exception ex)
                {
                    context.HandleError(ex, $"Writing destination property '{destProp.Name}' threw.");
                    if (context.Options.ThrowOnError) throw;
                }
            }
        }

        /// <summary>
        /// Converts a raw value for a specific destination property, honouring an optional
        /// attribute-level <see cref="MappingConverterAttribute"/> before falling back to the
        /// context's default conversion path.
        /// </summary>
        private static object? ConvertPropertyValue(object? rawValue, PropertyInfo destinationProperty, MappingContext context)
        {
            var attr = destinationProperty.GetCustomAttribute<MappingConverterAttribute>();
            if (attr != null)
            {
                var converter = (MappingConverter)Activator.CreateInstance(attr.ConverterType)!;
                try
                {
                    return converter.Convert(rawValue, null, context);
                }
                catch (Exception ex)
                {
                    context.HandleError(ex, $"Attribute converter '{attr.ConverterType.Name}' threw.");
                    if (context.Options.ThrowOnError) throw;
                    return MappingContext.GetDefault(destinationProperty.PropertyType);
                }
            }

            return Map(rawValue, destinationProperty.PropertyType, null, context);
        }

        private static PropertyInfo? FindSourceProperty(ObjectContract? sourceContract, object source, string name)
        {
            if (sourceContract != null)
            {
                foreach (var p in sourceContract.Properties)
                {
                    if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return p;
                    }
                }
                return null;
            }

            // Fallback: resolver did not treat source as an object contract (e.g., custom resolver).
            return source.GetType().GetProperty(name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        }

        private static void InvokeCustomMapping(
            PairConfiguration pairConfig,
            object source,
            object destination,
            MappingContext context,
            Type sourceType)
        {
            try
            {
                if (pairConfig.CustomMappingHasContext)
                {
                    pairConfig.CustomMapping!.DynamicInvoke(source, destination, context);
                }
                else
                {
                    pairConfig.CustomMapping!.DynamicInvoke(source, destination);
                }
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                context.HandleError(ex.InnerException, "CustomMapping threw.");
                if (context.Options.ThrowOnError)
                {
                    throw new MappingException(
                        $"Custom mapping failed for {sourceType.Name}→{destination.GetType().Name}.",
                        ex.InnerException);
                }
            }
            catch (Exception ex)
            {
                context.HandleError(ex, "CustomMapping threw.");
                if (context.Options.ThrowOnError)
                {
                    throw new MappingException(
                        $"Custom mapping failed for {sourceType.Name}→{destination.GetType().Name}.",
                        ex);
                }
            }
        }

        private static void RunPairHooks(
            List<Delegate> hooks,
            object source,
            object destination,
            MappingContext context)
        {
            foreach (var hook in hooks)
            {
                try
                {
                    var parameters = hook.Method.GetParameters();
                    if (parameters.Length == 3)
                    {
                        hook.DynamicInvoke(source, destination, context);
                    }
                    else
                    {
                        hook.DynamicInvoke(source, destination);
                    }
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    context.HandleError(ex.InnerException, "Pair hook threw.");
                    if (context.Options.ThrowOnError) throw ex.InnerException;
                }
                catch (Exception ex)
                {
                    context.HandleError(ex, "Pair hook threw.");
                    if (context.Options.ThrowOnError) throw;
                }
            }
        }
    }
}
