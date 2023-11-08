using System;
using System.Collections.Generic;
using TransparentValueObjects.Augments;
using TransparentValueObjects.Generated;

namespace TransparentValueObjects.Sample;

[ValueObject<string>]
public readonly partial struct SampleValueObjectString :
    IHasDefaultValue<SampleValueObjectString, string>,
    IHasDefaultEqualityComparer<SampleValueObjectString, string>,
    IHasRandomValueGenerator<SampleValueObjectString, string, Random>
{
    public static SampleValueObjectString DefaultValue => From("Hello World!");
    public static IEqualityComparer<string> InnerValueDefaultEqualityComparer => StringComparer.OrdinalIgnoreCase;
    public static Func<Random?, SampleValueObjectString> GenerateRandomValue => random =>
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return From(string.Create(10, random ?? new Random(), static (span, random) =>
        {
            for (var i = 0; i < span.Length; i++)
                span[i] = chars[random.Next(0, chars.Length)];
        }));
    };
}