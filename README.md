# MorphMapping

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A lightweight, reflection-based object mapper for .NET with customizable converters, per-pair hooks and a DI-friendly configuration model.

The library itself has no dependency on `Microsoft.Extensions.DependencyInjection` — it ships as two packages:

- **`MorphMapping`** — the core mapper. Standalone, depends only on `Microsoft.Extensions.Logging.Abstractions`.
- **`MorphMapping.DependencyInjection`** — integration with `Microsoft.Extensions.DependencyInjection`.

## Features

- Property-name mapping (case-insensitive) with support for nested objects, collections, arrays and dictionaries.
- Standalone configuration via `MapperBuilder`, or through DI (`services.AddMorphMapper(...)`).
- Property renaming and ignoring (including the `[IgnoreMapping]` attribute).
- Custom value providers for specific destination properties.
- `Before`/`After` hooks at both per-pair and global levels.
- Custom converters: global (`MapperOptions.Converters`), per-property *and* per-class via `[MappingConverter(typeof(MyConverter))]`.
- Constructor-based mapping by parameter name.
- Built-in support for `Nullable<T>`, enums (string↔enum, int↔enum), numeric conversions, `IDictionary`, `IEnumerable`.
- `MappingContext` to pass arbitrary state through the mapping pipeline.

## Build and test

```bash
dotnet restore MorphMapping.sln
dotnet build MorphMapping.sln -c Release
dotnet test MorphMapping.sln -c Release
```

## Usage

### Standalone (no DI)

```csharp
using MorphMapping;

var mapper = new MapperBuilder()
    .ConfigureOptions(opts => opts.ThrowOnError = true)
    .Configure<SourcePerson, DestPerson>(cfg => cfg
        .MapProperty(nameof(SourcePerson.Name), nameof(DestPerson.FullName))
        .IgnoreProperty(nameof(DestPerson.SecretCode))
        .AfterMapping((src, dst) => dst.FullName = dst.FullName?.Trim()))
    .Build();

var dest = mapper.Map<DestPerson>(new SourcePerson { Name = "Alice", Age = 30 });
```

### With `Microsoft.Extensions.DependencyInjection`

Install `MorphMapping.DependencyInjection` and register the mapper as a singleton.
`AddMorphMapper` takes an optional `Action<MapperOptions>` for options-level
configuration and **returns the `MapperBuilder`** so per-pair configuration can
be chained fluently:

```csharp
using Microsoft.Extensions.DependencyInjection;
using MorphMapping;
using MorphMapping.DependencyInjection;

var services = new ServiceCollection();
services.AddLogging();

services
    .AddMorphMapper(opts =>
    {
        opts.ThrowOnError = true;
        opts.Converters.Add(new MoneyToDtoConverter());
    })
    .Configure<SourcePerson, DestPerson>(cfg => cfg
        .MapProperty(nameof(SourcePerson.Name), nameof(DestPerson.FullName))
        .IgnoreProperty(nameof(DestPerson.SecretCode)));

var provider = services.BuildServiceProvider();
var mapper = provider.GetRequiredService<IMapper>();
```

The options-level lambda runs first (before any builder actions) and always
sees a fresh `MapperOptions`. Any `.Configure<...>()` chained after
`AddMorphMapper` is picked up lazily when the mapper is materialized from the
container, so call order is forgiving.

### Mapping into an existing instance

```csharp
var existing = new DestPerson { FullName = "preserved" };
mapper.Map(source, existing);
```

### Passing context

```csharp
var dest = mapper.Map<DestPerson>(source, ctx => ctx.Add("culture", "ru-RU"));
```

## Advanced scenarios

### Custom value provider for a destination property

```csharp
builder.Configure<SourcePerson, DestPerson>(cfg => cfg
    .MapProperty(nameof(DestPerson.FullName), src => $"{src.Name} ({src.Age})"));
```

### Before / After hooks

```csharp
builder.Configure<SourcePerson, DestPerson>(cfg => cfg
    .BeforeMapping((src, dst) => { /* ... */ })
    .AfterMapping((src, dst, ctx) => { /* ... */ }));
```

### Full custom mapping logic

```csharp
builder.Configure<SourcePerson, DestPerson>(cfg => cfg
    .CustomMapping((src, dst) =>
    {
        dst.Name = src.Name.ToUpperInvariant();
        dst.Age = src.Age;
    }));
```

### Global hooks

```csharp
builder
    .GlobalBeforeMapping((s, d) => { /* audit */ })
    .GlobalAfterMapping((s, d) => { /* log */ });
```

### Global converter for a type

```csharp
public sealed class MoneyToDtoConverter : MappingConverter<Money, MoneyDto>
{
    public override MoneyDto? Convert(Money? source, MoneyDto? destination, MappingContext context)
    {
        if (source is null) return null;
        var dto = destination ?? new MoneyDto();
        dto.Formatted = $"{source.Amount:F2} {source.Currency}";
        return dto;
    }
}

services.AddMorphMapper(opts => opts.Converters.Add(new MoneyToDtoConverter()));
```

### Per-property converter via attribute

```csharp
public sealed class UpperCaseConverter : MappingConverter<string, string>
{
    public override string? Convert(string? source, string? destination, MappingContext context)
        => source?.ToUpperInvariant();
}

public class DestDto
{
    [MappingConverter(typeof(UpperCaseConverter))]
    public string Title { get; set; } = string.Empty;
}
```

### Per-class converter via attribute

`[MappingConverter]` can also be placed on a *class*. When the pipeline targets
that class as a destination (root-level, nested property, collection item),
the attached converter is used instead of the default object-copy flow — no
global registration required. The attribute can equally be placed on a source
class to redirect everything *leaving* that type through a converter.

Precedence, from most- to least-specific:

1. per-property `[MappingConverter]` on the destination property
2. per-class `[MappingConverter]` on the destination type
3. per-class `[MappingConverter]` on the source type
4. global converters from `MapperOptions.Converters`
5. default contract (`ObjectContract`, `EnumContract`, etc.)

```csharp
public sealed class MoneyToDtoConverter : MappingConverter<Money, MoneyDto>
{
    public override MoneyDto? Convert(Money? source, MoneyDto? destination, MappingContext context)
    {
        if (source is null) return null;
        var dto = destination ?? new MoneyDto();
        dto.Formatted = $"{source.Amount:F2} {source.Currency}";
        return dto;
    }
}

// Anything mapped *into* MoneyDto (root or nested) uses MoneyToDtoConverter.
[MappingConverter(typeof(MoneyToDtoConverter))]
public class MoneyDto
{
    public string Formatted { get; set; } = string.Empty;
}
```

The converter's declared `SourceType` / `DestinationType` still have to be
compatible with the actual runtime types — mismatched attributes are safely
ignored and the pipeline falls through to the next rule.

### Ignoring a property via attribute

```csharp
public class Dto
{
    public string Public { get; set; } = string.Empty;

    [IgnoreMapping]
    public string Secret { get; set; } = string.Empty;
}
```

## Mapper options (`MapperOptions`)

| Option | Default | Description |
|--------|---------|-------------|
| `ThrowOnError` | `false` | Re-throws exceptions from user actions, wrapped in `MappingException` where appropriate. |
| `LogErrors` | `true` | Logs mapping errors through `ILogger<Mapper>` (if a logger factory was provided). |
| `FallbackToParameterlessConstructor` | `true` | Falls back to the parameterless constructor when no parametric one matches. |
| `ContractResolver` | `DefaultContractResolver` | Resolver used to discover type contracts. |
| `Converters` | empty | List of global `MappingConverter` instances. |

## Built-in type support

- Primitives and `string` — via `Convert.ChangeType` and `Enum.Parse`.
- `Enum` — from string (`Enum.Parse`) and from an integer value (`Enum.ToObject`).
- `Nullable<T>` — unwrapped transparently to the underlying type.
- Arrays — sized from the source collection.
- `IEnumerable<T>`, `ICollection<T>`, `List<T>` — populated via `Add`.
- `IDictionary<TKey, TValue>` — iterated as `Key`/`Value` pairs and added via `Add(key, value)`.
- Arbitrary classes/structs — via `ObjectContract`: reflective property walk with support for constructor-by-name.
