using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MorphMapping
{
    /// <summary>
    /// Carries user state <i>and</i> mapping infrastructure through a mapping call. Users see
    /// only the key-value bag API and the <see cref="Mapper"/> reference; contracts (in the same
    /// assembly) reach the resolver, the object factory, pair-configuration lookups, global
    /// before/after hooks and the error sink.
    /// </summary>
    public sealed class MappingContext : ICloneable
    {
        private readonly Dictionary<string, object?> _items;
        private readonly ILogger _logger;

        internal MapperOptions Options { get; }
        internal IContractResolver Resolver { get; }
        internal IObjectFactory ObjectFactory { get; }

        /// <summary>The mapper that owns this mapping call.</summary>
        public IMapper Mapper { get; }

        /// <summary>Exposes a read-only snapshot of the user-state items.</summary>
        public IDictionary<string, object?> Items => _items;

        /// <summary>
        /// Public parameterless constructor. Creates an empty user-state bag without
        /// infrastructure — used by <see cref="Clone"/> and by callers who only need
        /// the key-value bag (e.g. inside <c>configureContext</c> callbacks).
        /// </summary>
        public MappingContext()
        {
            _items = new Dictionary<string, object?>(StringComparer.Ordinal);
            _logger = NullLogger.Instance;
            Options = null!;
            Resolver = null!;
            ObjectFactory = null!;
            Mapper = null!;
        }

        /// <summary>
        /// Internal constructor used by <see cref="MorphMapping.Mapper"/> to build a fully
        /// initialized context before the first conversion call.
        /// </summary>
        internal MappingContext(
            MapperOptions options,
            IContractResolver resolver,
            IObjectFactory objectFactory,
            ILogger logger,
            IMapper mapper)
        {
            _items = new Dictionary<string, object?>(StringComparer.Ordinal);
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            ObjectFactory = objectFactory ?? throw new ArgumentNullException(nameof(objectFactory));
            _logger = logger ?? NullLogger.Instance;
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// Private copy-constructor — copies user-state items and inherits infrastructure.
        /// </summary>
        private MappingContext(MappingContext source)
        {
            _items = new Dictionary<string, object?>(source._items, StringComparer.Ordinal);
            Options = source.Options;
            Resolver = source.Resolver;
            ObjectFactory = source.ObjectFactory;
            _logger = source._logger;
            Mapper = source.Mapper;
        }

        /// <summary>Adds a key-value pair to the context.</summary>
        public MappingContext Add(string key, object? value)
        {
            _ = key ?? throw new ArgumentNullException(nameof(key));
            _items[key] = value;
            return this;
        }

        /// <summary>Checks whether the context contains the specified key.</summary>
        public bool ContainsKey(string key)
        {
            _ = key ?? throw new ArgumentNullException(nameof(key));
            return _items.ContainsKey(key);
        }

        /// <summary>Gets a value by key; returns default if not present.</summary>
        public T? Get<T>(string key)
        {
            _ = key ?? throw new ArgumentNullException(nameof(key));
            if (_items.TryGetValue(key, out var value) && value is T typed)
            {
                return typed;
            }
            return default;
        }

        /// <summary>Tries to get a typed value by key.</summary>
        public bool TryGet<T>(string key, [MaybeNullWhen(false)] out T value)
        {
            _ = key ?? throw new ArgumentNullException(nameof(key));
            if (_items.TryGetValue(key, out var raw) && raw is T typed)
            {
                value = typed;
                return true;
            }
            value = default!;
            return false;
        }

        /// <summary>Removes a key from the context.</summary>
        public bool Remove(string key)
        {
            if (key is null) throw new ArgumentNullException(nameof(key));
            return _items.Remove(key);
        }

        /// <summary>Returns a shallow copy including user-state items and infrastructure references.</summary>
        public MappingContext Clone() => new MappingContext(this);

        /// <summary>
        /// Returns the per-pair configuration for <paramref name="sourceType"/> →
        /// <paramref name="destinationType"/>, or <c>null</c> if the user never configured this pair.
        /// </summary>
        internal PairConfiguration? GetPairConfiguration(Type sourceType, Type destinationType)
        {
            _ = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
            _ = destinationType ?? throw new ArgumentNullException(nameof(destinationType));
            Options.PairConfigurations.TryGetValue((sourceType, destinationType), out var config);
            return config;
        }

        /// <summary>Whether the pair has a user-defined custom mapping that replaces the default copy phase.</summary>
        internal bool HasCustomMapping(Type sourceType, Type destinationType)
        {
            var pair = GetPairConfiguration(sourceType, destinationType);
            return pair?.CustomMapping != null;
        }

        /// <summary>Runs all global before-mapping hooks. Invoked by the object contract.</summary>
        internal void RunGlobalBefore(object source, object destination)
        {
            foreach (var hook in Options.GlobalBeforeMappings)
            {
                try
                {
                    hook(source, destination, this);
                }
                catch (Exception ex)
                {
                    HandleError(ex, "Global before-hook threw.");
                    if (Options.ThrowOnError) throw;
                }
            }
        }

        /// <summary>Runs all global after-mapping hooks. Invoked by the object contract.</summary>
        internal void RunGlobalAfter(object source, object destination)
        {
            foreach (var hook in Options.GlobalAfterMappings)
            {
                try
                {
                    hook(source, destination, this);
                }
                catch (Exception ex)
                {
                    HandleError(ex, "Global after-hook threw.");
                    if (Options.ThrowOnError) throw;
                }
            }
        }

        /// <summary>Reports an error through the mapper's logger (honouring <see cref="MapperOptions.LogErrors"/>).</summary>
        internal void HandleError(Exception? exception, string message)
        {
            if (!Options.LogErrors) return;

            if (exception == null)
                _logger.LogError("{Message}", message);
            else
                _logger.LogError(exception, "{Message}", message);
        }

        internal static object? GetDefault(Type type)
        {
            if (type.IsValueType && Nullable.GetUnderlyingType(type) is null)
            {
                try
                {
                    return Activator.CreateInstance(type);
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        #region ICloneable
        object ICloneable.Clone() => Clone();
        #endregion
    }
}
