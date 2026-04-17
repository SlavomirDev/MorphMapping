using System;
using System.Collections.Generic;

namespace MorphMapping.Tests.Models
{
    public class SourcePerson
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public DateTime Birthday { get; set; }
        public SourceAddress? Address { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public string? SecretCode { get; set; }
        public string NumericString { get; set; } = "42";
    }

    public class DestPerson
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public DateTime Birthday { get; set; }
        public DestAddress? Address { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public string? SecretCode { get; set; }
        public int NumericString { get; set; }
        public string? FullName { get; set; }
    }

    public class SourceAddress
    {
        public string City { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
    }

    public class DestAddress
    {
        public string City { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
    }

    public enum Color
    {
        Red = 1,
        Green = 2,
        Blue = 3
    }

    public class SourceWithEnum
    {
        public string Color { get; set; } = "Red";
        public int StatusValue { get; set; } = 2;
    }

    public class DestWithEnum
    {
        public Color Color { get; set; }
        public Color StatusValue { get; set; }
    }

    public class SourceWithIgnore
    {
        public string Public { get; set; } = "public";
        public string Private { get; set; } = "private";
    }

    public class DestWithIgnore
    {
        public string Public { get; set; } = string.Empty;

        [MappingIgnore]
        public string Private { get; set; } = "default-value";
    }

    public class SourceArray
    {
        public int[] Numbers { get; set; } = Array.Empty<int>();
    }

    public class DestArray
    {
        public int[] Numbers { get; set; } = Array.Empty<int>();
    }

    public class SourceDict
    {
        public Dictionary<string, int> Scores { get; set; } = new Dictionary<string, int>();
    }

    public class DestDict
    {
        public Dictionary<string, int> Scores { get; set; } = new Dictionary<string, int>();
    }

    public class SourceNullable
    {
        public int? Value { get; set; }
    }

    public class DestNullable
    {
        public int? Value { get; set; }
        public int ValueAsNonNullable { get; set; }
    }

    public class SourceCtor
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class DestCtor
    {
        public int Id { get; }
        public string Name { get; }

        public DestCtor(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    // Used for global converter tests
    public class Money
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";

        public override bool Equals(object? obj) =>
            obj is Money m && m.Amount == Amount && m.Currency == Currency;

        public override int GetHashCode() => HashCode.Combine(Amount, Currency);
    }

    public class MoneyDto
    {
        public string Formatted { get; set; } = string.Empty;
    }

    public class MoneyToDtoConverter : MappingConverter<Money, MoneyDto>
    {
        public override MoneyDto? Convert(Money? source, MoneyDto? destination, MappingContext context)
        {
            if (source is null) return null;
            var dest = destination ?? new MoneyDto();
            dest.Formatted = $"{source.Amount:F2} {source.Currency}";
            return dest;
        }
    }

    public class SourceWithMoney
    {
        public Money Price { get; set; } = new Money();
    }

    public class DestWithMoneyDto
    {
        public MoneyDto? Price { get; set; }
    }

    // Attribute-based converter
    public class UpperCaseConverter : MappingConverter<string, string>
    {
        public override string? Convert(string? source, string? destination, MappingContext context) =>
            source?.ToUpperInvariant();
    }

    public class SourceWithAttr
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
    }

    public class DestWithAttr
    {
        [MappingConverter(typeof(UpperCaseConverter))]
        public string Title { get; set; } = string.Empty;

        public string Subtitle { get; set; } = string.Empty;
    }
}
