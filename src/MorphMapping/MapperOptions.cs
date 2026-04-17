using System;
using System.Collections.Generic;

namespace MorphMapping
{
    /// <summary>Root configuration for <see cref="IMapper"/>.</summary>
    public sealed class MapperOptions
    {
        /// <summary>
        /// If <c>true</c>, exceptions thrown from user hooks and custom mappings are wrapped into
        /// <see cref="MappingException"/> and re-thrown. Defaults to <c>false</c>.
        /// </summary>
        public bool ThrowOnError { get; set; } = false;

        /// <summary>
        /// If <c>true</c>, errors occurring inside the pipeline are written via the attached
        /// <see cref="Microsoft.Extensions.Logging.ILogger"/>. Defaults to <c>true</c>.
        /// </summary>
        public bool LogErrors { get; set; } = true;

        /// <summary>
        /// If <c>true</c>, falls back to a parameterless constructor when no parametric constructor
        /// matches the source. Defaults to <c>true</c>.
        /// </summary>
        public bool FallbackToParameterlessConstructor { get; set; } = true;

        /// <summary>Resolver used to discover types' contracts. Defaults to <see cref="DefaultContractResolver"/>.</summary>
        public IContractResolver ContractResolver { get; set; } = new DefaultContractResolver();

        /// <summary>
        /// Factory used to instantiate destination objects. Defaults to
        /// <see cref="ReflectionObjectFactory"/>. Replace to avoid reflection overhead
        /// (e.g. pre-compiled delegates, source generators, DI-resolved instances).
        /// </summary>
        public IObjectFactory ObjectFactory { get; set; } = ReflectionObjectFactory.Instance;

        /// <summary>Globally applied converters (ordered, first match wins).</summary>
        public List<MappingConverter> Converters { get; } = new List<MappingConverter>();

        /// <summary>Per-pair configurations; keys are <c>(TSource, TDestination)</c> tuples.</summary>
        internal Dictionary<(Type, Type), PairConfiguration> PairConfigurations { get; } =
            new Dictionary<(Type, Type), PairConfiguration>();

        /// <summary>Global before-mapping hooks.</summary>
        internal List<GlobalMappingContextAction> GlobalBeforeMappings { get; } =
            new List<GlobalMappingContextAction>();

        /// <summary>Global after-mapping hooks.</summary>
        internal List<GlobalMappingContextAction> GlobalAfterMappings { get; } =
            new List<GlobalMappingContextAction>();
    }

    /// <summary>Internal per-pair configuration state.</summary>
    internal sealed class PairConfiguration
    {
        public Dictionary<string, string> PropertyMappings { get; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, Delegate> ValueProviders { get; } =
            new Dictionary<string, Delegate>(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> IgnoredProperties { get; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public List<Delegate> BeforeMappings { get; } = new List<Delegate>();
        public List<Delegate> AfterMappings { get; } = new List<Delegate>();

        public Delegate? CustomMapping { get; set; }
        public bool CustomMappingHasContext { get; set; }
    }
}
