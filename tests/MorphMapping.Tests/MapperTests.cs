using Microsoft.Extensions.DependencyInjection;

using MorphMapping.DependencyInjection;
using MorphMapping.Tests.Models;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;

using Xunit;

namespace MorphMapping.Tests
{
    public class MapperTests
    {
        private static IMapper BuildMapper(Action<MapperBuilder>? configure = null, Action<MapperOptions>? options = null)
        {
            var services = new ServiceCollection();
            services.AddLogging();

            // AddMorphMapper now returns the MapperBuilder directly so fluent per-pair
            // configuration can be chained after options-level configuration.
            var builder = services.AddMorphMapper(options);
            configure?.Invoke(builder);

            var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IMapper>();
        }

        [Fact]
        public void Map_CopiesMatchingProperties_ByName()
        {
            var mapper = BuildMapper();
            var src = new SourcePerson
            {
                Id = 5,
                Name = "Alice",
                Age = 30,
                Birthday = new DateTime(1995, 5, 5),
            };

            var dest = mapper.Map<DestPerson>(src);

            Assert.Equal(5, dest.Id);
            Assert.Equal("Alice", dest.Name);
            Assert.Equal(30, dest.Age);
            Assert.Equal(new DateTime(1995, 5, 5), dest.Birthday);
        }

        [Fact]
        public void Map_ConvertsNestedObject()
        {
            var mapper = BuildMapper();
            var src = new SourcePerson
            {
                Address = new SourceAddress { City = "Moscow", Street = "Tverskaya" },
            };

            var dest = mapper.Map<DestPerson>(src);

            Assert.NotNull(dest.Address);
            Assert.Equal("Moscow", dest.Address!.City);
            Assert.Equal("Tverskaya", dest.Address.Street);
        }

        [Fact]
        public void Map_ConvertsStringToInt_ViaPrimitiveContract()
        {
            var mapper = BuildMapper();
            var src = new SourcePerson { NumericString = "123" };

            var dest = mapper.Map<DestPerson>(src);

            Assert.Equal(123, dest.NumericString);
        }

        [Fact]
        public void Map_ThrowsOnNullSource()
        {
            var mapper = BuildMapper();
            Assert.Throws<ArgumentNullException>(() => mapper.Map<DestPerson>(null!));
        }

        [Fact]
        public void Configure_MapProperty_RemapsSourceToDifferentDestination()
        {
            var mapper = BuildMapper(b =>
                b.Configure<SourcePerson, DestPerson>(cfg =>
                    cfg.MapProperty(nameof(SourcePerson.Name), nameof(DestPerson.FullName))));

            var dest = mapper.Map<DestPerson>(new SourcePerson { Name = "Bob" });

            Assert.Equal("Bob", dest.FullName);
        }

        [Fact]
        public void Configure_IgnoreProperty_DoesNotOverrideExistingValue()
        {
            var mapper = BuildMapper(b =>
                b.Configure<SourcePerson, DestPerson>(cfg =>
                    cfg.IgnoreProperty(nameof(DestPerson.SecretCode))));

            var dest = mapper.Map<DestPerson>(new SourcePerson { SecretCode = "hidden" });

            Assert.Null(dest.SecretCode);
        }

        [Fact]
        public void IgnoreMappingAttribute_SkipsProperty()
        {
            var mapper = BuildMapper();
            var dest = new DestWithIgnore();
            mapper.Map(new SourceWithIgnore(), dest);

            Assert.Equal("public", dest.Public);
            Assert.Equal("default-value", dest.Private);
        }

        [Fact]
        public void Configure_MapPropertyWithValueProvider_UsesCustomComputation()
        {
            var mapper = BuildMapper(b =>
                b.Configure<SourcePerson, DestPerson>(cfg =>
                    cfg.MapProperty(nameof(DestPerson.FullName), src => $"{src.Name} ({src.Age})")));

            var dest = mapper.Map<DestPerson>(new SourcePerson { Name = "Eve", Age = 29 });

            Assert.Equal("Eve (29)", dest.FullName);
        }

        [Fact]
        public void Configure_BeforeMapping_RunsBeforeCopy()
        {
            var ran = false;
            var mapper = BuildMapper(b =>
                b.Configure<SourcePerson, DestPerson>(cfg =>
                    cfg.BeforeMapping((src, dst) => { ran = true; })));

            mapper.Map<DestPerson>(new SourcePerson());

            Assert.True(ran);
        }

        [Fact]
        public void Configure_AfterMapping_CanMutateDestination()
        {
            var mapper = BuildMapper(b =>
                b.Configure<SourcePerson, DestPerson>(cfg =>
                    cfg.AfterMapping((src, dst) => dst.FullName = "AFTER")));

            var dest = mapper.Map<DestPerson>(new SourcePerson { Name = "X" });

            Assert.Equal("AFTER", dest.FullName);
        }

        [Fact]
        public void Configure_CustomMapping_ReplacesDefaultCopy()
        {
            var mapper = BuildMapper(b =>
                b.Configure<SourcePerson, DestPerson>(cfg =>
                    cfg.CustomMapping((src, dst) =>
                    {
                        dst.Name = src.Name.ToUpperInvariant();
                        dst.Age = -1;
                    })));

            var dest = mapper.Map<DestPerson>(new SourcePerson { Name = "alice", Age = 30 });

            Assert.Equal("ALICE", dest.Name);
            Assert.Equal(-1, dest.Age);
        }

        [Fact]
        public void GlobalBeforeAndAfter_Run_ForEveryMapping()
        {
            var before = 0;
            var after = 0;
            var mapper = BuildMapper(b => b
                .GlobalBeforeMapping((s, d) => before++)
                .GlobalAfterMapping((s, d) => after++));

            mapper.Map<DestPerson>(new SourcePerson());
            mapper.Map<DestAddress>(new SourceAddress());

            Assert.Equal(2, before);
            Assert.Equal(2, after);
        }

        [Fact]
        public void Map_ExistingDestination_PopulatesInPlace()
        {
            var mapper = BuildMapper();
            var existing = new DestPerson { FullName = "keep-me" };

            var result = mapper.Map(new SourcePerson { Name = "Sam" }, existing);

            Assert.Same(existing, result);
            Assert.Equal("Sam", existing.Name);
            Assert.Equal("keep-me", existing.FullName);
        }

        [Fact]
        public void Enum_StringToEnum_Works()
        {
            var mapper = BuildMapper();
            var dest = mapper.Map<DestWithEnum>(new SourceWithEnum { Color = "Blue", StatusValue = 2 });

            Assert.Equal(Color.Blue, dest.Color);
            Assert.Equal(Color.Green, dest.StatusValue);
        }

        [Fact]
        public void Arrays_Roundtrip()
        {
            var mapper = BuildMapper();
            var dest = mapper.Map<DestArray>(new SourceArray { Numbers = new[] { 1, 2, 3 } });

            Assert.Equal(new[] { 1, 2, 3 }, dest.Numbers);
        }

        [Fact]
        public void Dictionaries_Roundtrip()
        {
            var mapper = BuildMapper();
            var dest = mapper.Map<DestDict>(new SourceDict
            {
                Scores = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 },
            });

            Assert.Equal(2, dest.Scores.Count);
            Assert.Equal(1, dest.Scores["a"]);
            Assert.Equal(2, dest.Scores["b"]);
        }

        [Fact]
        public void Collections_ListOfStrings_Roundtrip()
        {
            var mapper = BuildMapper();
            var src = new SourcePerson { Tags = new List<string> { "a", "b", "c" } };

            var dest = mapper.Map<DestPerson>(src);

            Assert.Equal(new[] { "a", "b", "c" }, dest.Tags);
        }

        [Fact]
        public void Nullable_ValueTypes_Handled()
        {
            var mapper = BuildMapper();
            var dest1 = mapper.Map<DestNullable>(new SourceNullable { Value = 42 });
            Assert.Equal(42, dest1.Value);
            Assert.Equal(0, dest1.ValueAsNonNullable);

            var dest2 = mapper.Map<DestNullable>(new SourceNullable { Value = null });
            Assert.Null(dest2.Value);
            Assert.Equal(0, dest2.ValueAsNonNullable);
        }

        [Fact]
        public void Ctor_ParameterMatching_PicksWidestConstructor()
        {
            var mapper = BuildMapper();
            var dest = mapper.Map<DestCtor>(new SourceCtor { Id = 7, Name = "hello" });

            Assert.Equal(7, dest.Id);
            Assert.Equal("hello", dest.Name);
        }

        [Fact]
        public void GlobalConverter_ByType_IsHonoredDuringPropertyMapping()
        {
            var mapper = BuildMapper(options: opts => opts.Converters.Add(new MoneyToDtoConverter()));
            var dest = mapper.Map<DestWithMoneyDto>(new SourceWithMoney
            {
                Price = new Money { Amount = 9.99m, Currency = "EUR" },
            });

            Assert.NotNull(dest.Price);
            Assert.Equal("9,99 EUR", dest.Price!.Formatted);
        }

        [Fact]
        public void AttributeConverter_IsAppliedToDecoratedProperty()
        {
            var mapper = BuildMapper();
            var dest = mapper.Map<DestWithAttr>(new SourceWithAttr { Title = "hello", Subtitle = "world" });

            Assert.Equal("HELLO", dest.Title);
            Assert.Equal("world", dest.Subtitle);
        }

        [Fact]
        public void ClassLevelConverter_OnDestination_IsUsedAtRoot()
        {
            var mapper = BuildMapper();
            var dto = mapper.Map<MoneyDtoClassAttr>(new Money { Amount = 9.99m, Currency = "EUR" });

            Assert.Equal("9,99 EUR (class)", dto.Formatted);
        }

        [Fact]
        public void ClassLevelConverter_OnDestination_IsUsedForNestedProperty()
        {
            var mapper = BuildMapper();
            var dest = mapper.Map<DestWithClassAttrNested>(new SourceWithMoney
            {
                Price = new Money { Amount = 1.5m, Currency = "USD" },
            });

            Assert.NotNull(dest.Price);
            Assert.Equal("1,50 USD (class)", dest.Price!.Formatted);
        }

        [Fact]
        public void ClassLevelConverter_OnSource_IsUsedAtRoot()
        {
            var mapper = BuildMapper();
            var dto = mapper.Map<MoneyDto>(new LegacyMoney { Amount = 42.0m, Currency = "PLN" });

            Assert.Equal("LEGACY 42,00 PLN", dto.Formatted);
        }

        [Fact]
        public void ClassLevelConverter_WinsOverMatchingGlobalConverter()
        {
            // Register a global Money→MoneyDtoClassAttr converter, then confirm the class-level
            // attribute converter on MoneyDtoClassAttr still wins.
            var mapper = BuildMapper(options: opts => opts.Converters.Add(new GlobalMoneyToDtoClassAttrConverter()));
            var dto = mapper.Map<MoneyDtoClassAttr>(new Money { Amount = 7m, Currency = "CHF" });

            Assert.Equal("7,00 CHF (class)", dto.Formatted);
        }

        [Fact]
        public void Context_CanPassAdditionalData()
        {
            var mapper = BuildMapper(b =>
                b.Configure<SourcePerson, DestPerson>(cfg =>
                    cfg.AfterMapping((src, dst, ctx) =>
                    {
                        if (ctx.TryGet<string>("prefix", out var prefix))
                            dst.FullName = prefix + dst.Name;
                    })));

            var dest = mapper.Map<DestPerson>(new SourcePerson { Name = "Joe" }, ctx => ctx.Add("prefix", "Mr. "));

            Assert.Equal("Mr. Joe", dest.FullName);
        }

        [Fact]
        public void MapperOptions_ThrowOnError_PropagatesException()
        {
            var mapper = BuildMapper(
                b => b.Configure<SourcePerson, DestPerson>(cfg =>
                    cfg.AfterMapping((src, dst) => throw new InvalidOperationException("boom"))),
                opts => opts.ThrowOnError = true);

            Assert.Throws<InvalidOperationException>(() => mapper.Map<DestPerson>(new SourcePerson()));
        }

        [Fact]
        public void Map_UsesFirstMatchingGlobalConverter_OnRootCall()
        {
            var mapper = BuildMapper(options: opts => opts.Converters.Add(new MoneyToDtoConverter()));
            var dto = mapper.Map<MoneyDto>(new Money { Amount = 1.5m, Currency = "USD" });

            Assert.Equal("1,50 USD", dto.Formatted);
        }

        [Fact]
        public void MappingContext_Clone_CopiesItems()
        {
            var mapper = BuildMapper();
            mapper.Map<DestPerson>(new SourcePerson(), ctx =>
            {
                ctx.Add("k", "v");
                var clone = ctx.Clone();
                Assert.True(clone.ContainsKey("k"));
                Assert.Equal("v", clone.Get<string>("k"));
            });
        }

        [Fact]
        public void MappingException_CustomMapping_FailureIsWrapped_WhenThrowOnError()
        {
            var mapper = BuildMapper(
                b => b.Configure<SourcePerson, DestPerson>(cfg =>
                    cfg.CustomMapping((s, d) => throw new InvalidOperationException("x"))),
                opts => opts.ThrowOnError = true);

            Assert.Throws<MappingException>(() => mapper.Map<DestPerson>(new SourcePerson()));
        }

        [Fact]
        public void MapperBuilder_CanBuildStandalone_WithoutDI()
        {
            var mapper = new MapperBuilder()
                .Configure<SourcePerson, DestPerson>(cfg =>
                    cfg.MapProperty(nameof(SourcePerson.Name), nameof(DestPerson.FullName)))
                .Build();

            var dest = mapper.Map<DestPerson>(new SourcePerson { Name = "Standalone" });
            Assert.Equal("Standalone", dest.FullName);
        }
    }
}
