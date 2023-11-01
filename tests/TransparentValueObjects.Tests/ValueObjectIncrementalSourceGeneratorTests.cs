using System.Linq;
using System.Text;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TransparentValueObjects.Augments;
using Xunit;

namespace TransparentValueObjects.Tests;

public class ValueObjectIncrementalSourceGeneratorTests
{
    private const string Input =
"""
namespace TestNamespace;

[TransparentValueObjects.Generated.ValueObject<string>]
public readonly partial struct SampleValueObject :
    TransparentValueObjects.Augments.IHasDefaultValue<SampleValueObject, string>
    TransparentValueObjects.Augments.IHasDefaultEqualityComparer<SampleValueObject, string>
{
    public static SampleValueObject GetDefaultValue() => From("Hello World!");
    public static IEqualityComparer<string> InnerValueDefaultEqualityComparer => StringComparer.OrdinalIgnoreCase;
}
""";

    private const string Output =
"""
// <auto-generated/>
#nullable enable
namespace TestNamespace;

[global::System.Diagnostics.DebuggerDisplay("{Value}")]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage(Justification = "Auto-generated.")]
readonly partial struct SampleValueObject :
    global::TransparentValueObjects.Augments.IValueObject<global::System.String>,
	global::System.IEquatable<SampleValueObject>,
	global::System.IEquatable<global::System.String>
{
	public readonly global::System.String Value;

    public SampleValueObject()
    {
        Value = DefaultValue.Value;
    }

	private SampleValueObject(global::System.String value)
	{
		Value = value;
	}

	public static SampleValueObject From(global::System.String value) => new(value);

	public override int GetHashCode() => Value.GetHashCode();

	public override string ToString() => Value.ToString();

	public bool Equals(SampleValueObject other) => Equals(other.Value);
	public bool Equals(global::System.String? other) => InnerValueDefaultEqualityComparer.Equals(Value, other);
	public bool Equals(SampleValueObject other, global::System.Collections.Generic.IEqualityComparer<global::System.String> comparer) => comparer.Equals(Value, other.Value);
	public override bool Equals(object? obj)
	{
		if (obj is null) return false;
		if (obj is SampleValueObject value) return Equals(value);
		if (obj is global::System.String innerValue) return Equals(innerValue);
		return false;
	}

	public static bool operator ==(SampleValueObject left, SampleValueObject right) => left.Equals(right);
	public static bool operator !=(SampleValueObject left, SampleValueObject right) => !left.Equals(right);

	public static bool operator ==(SampleValueObject left, global::System.String right) => left.Equals(right);
	public static bool operator !=(SampleValueObject left, global::System.String right) => !left.Equals(right);

	public static bool operator ==(global::System.String left, SampleValueObject right) => right.Equals(left);
	public static bool operator !=(global::System.String left, SampleValueObject right) => !right.Equals(left);

	public static explicit operator SampleValueObject(global::System.String value) => From(value);
	public static explicit operator global::System.String(SampleValueObject value) => value.Value;

}
""";

    [Fact]
    public void TestGenerator()
    {
        var generator = new ValueObjectIncrementalSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(nameof(ValueObjectIncrementalSourceGeneratorTests),
            new[] { CSharpSyntaxTree.ParseText(Input) },
            new[]
            {
                // To support 'System.Attribute' inheritance, add reference to 'System.Private.CoreLib'.
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),

                // augments
                MetadataReference.CreateFromFile(typeof(Marker).Assembly.Location)
            });

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        var generated = runResult.GeneratedTrees.FirstOrDefault(t => t.FilePath.EndsWith("SampleValueObject.g.cs"));
        generated.Should().NotBeNull();

        NormalizeEquals(generated!.GetText().ToString(), Output);
    }

    [Fact]
    public void Test_AddPublicConstructor_WithoutDefaultValue()
    {
        const string valueObjectTypeName = "MyValueObject";
        const string output =
$$"""
[global::System.Obsolete($"Use {{valueObjectTypeName}}.{nameof(From)} instead.", error: true)]
public {{valueObjectTypeName}}()
{
    throw new global::System.InvalidOperationException($"Use {{valueObjectTypeName}}.{nameof(From)} instead.");
}
""";

        var cw = new CodeWriter();
        ValueObjectIncrementalSourceGenerator.AddPublicConstructor(cw, valueObjectTypeName, hasDefaultValue: false);

        NormalizeEquals(cw.ToString(), output);
    }

    [Fact]
    public void Test_AddPublicConstructor_WithDefaultValue()
    {
        const string valueObjectTypeName = "MyValueObject";
        const string output =
$$"""
public {{valueObjectTypeName}}()
{
    Value = DefaultValue.Value;
}
""";

        var cw = new CodeWriter();
        ValueObjectIncrementalSourceGenerator.AddPublicConstructor(cw, valueObjectTypeName, hasDefaultValue: true);

        NormalizeEquals(cw.ToString(), output);
    }

    [Fact]
    public void Test_AddPrivateConstructor()
    {
        const string valueObjectTypeName = "MyValueObject";
        const string innerValueTypeName = "string";
        const string output =
$$"""
private {{valueObjectTypeName}}({{innerValueTypeName}} value)
{
    Value = value;
}
""";

        var cw = new CodeWriter();
        ValueObjectIncrementalSourceGenerator.AddPrivateConstructor(cw, valueObjectTypeName, innerValueTypeName);

        NormalizeEquals(cw.ToString(), output);
    }

    [Fact]
    public void Test_OverrideBaseMethods()
    {
        const string output =
"""
public override int GetHashCode() => Value.GetHashCode();

public override string ToString() => Value.ToString();

""";

        var cw = new CodeWriter();
        ValueObjectIncrementalSourceGenerator.OverrideBaseMethods(cw);

        NormalizeEquals(cw.ToString(), output);
    }

    [Fact]
    public void Test_ImplementEqualsMethods_WithoutDefaultEqualityComparer()
    {
        const string valueObjectTypeName = "MyValueObject";
        const string innerValueTypeName = "string";
        const string output =
$$"""
public bool Equals({{valueObjectTypeName}} other) => Equals(other.Value);
public bool Equals({{innerValueTypeName}}? other) => Value.Equals(other);
public bool Equals({{valueObjectTypeName}} other, global::System.Collections.Generic.IEqualityComparer<{{innerValueTypeName}}> comparer) => comparer.Equals(Value, other.Value);
public override bool Equals(object? obj)
{
	if (obj is null) return false;
	if (obj is {{valueObjectTypeName}} value) return Equals(value);
	if (obj is {{innerValueTypeName}} innerValue) return Equals(innerValue);
	return false;
}
""";

        var cw = new CodeWriter();
        ValueObjectIncrementalSourceGenerator.ImplementEqualsMethods(cw, valueObjectTypeName, innerValueTypeName, "?", hasDefaultEqualityComparer: false);

        NormalizeEquals(cw.ToString(), output);
    }

    [Fact]
    public void Test_ImplementEqualsMethods_WithDefaultEqualityComparer()
    {
        const string valueObjectTypeName = "MyValueObject";
        const string innerValueTypeName = "string";
        const string output =
$$"""
public bool Equals({{valueObjectTypeName}} other) => Equals(other.Value);
public bool Equals({{innerValueTypeName}}? other) => InnerValueDefaultEqualityComparer.Equals(Value, other);
public bool Equals({{valueObjectTypeName}} other, global::System.Collections.Generic.IEqualityComparer<{{innerValueTypeName}}> comparer) => comparer.Equals(Value, other.Value);
public override bool Equals(object? obj)
{
	if (obj is null) return false;
	if (obj is {{valueObjectTypeName}} value) return Equals(value);
	if (obj is {{innerValueTypeName}} innerValue) return Equals(innerValue);
	return false;
}
""";

        var cw = new CodeWriter();
        ValueObjectIncrementalSourceGenerator.ImplementEqualsMethods(cw, valueObjectTypeName, innerValueTypeName, "?", hasDefaultEqualityComparer: true);

        NormalizeEquals(cw.ToString(), output);
    }

    [Fact]
    public void Test_AddEqualityOperators()
    {
        const string valueObjectTypeName = "MyValueObject";
        const string innerValueTypeName = "string";
        const string output =
$$"""
public static bool operator ==({{valueObjectTypeName}} left, {{valueObjectTypeName}} right) => left.Equals(right);
public static bool operator !=({{valueObjectTypeName}} left, {{valueObjectTypeName}} right) => !left.Equals(right);

public static bool operator ==({{valueObjectTypeName}} left, {{innerValueTypeName}} right) => left.Equals(right);
public static bool operator !=({{valueObjectTypeName}} left, {{innerValueTypeName}} right) => !left.Equals(right);

public static bool operator ==({{innerValueTypeName}} left, {{valueObjectTypeName}} right) => right.Equals(left);
public static bool operator !=({{innerValueTypeName}} left, {{valueObjectTypeName}} right) => !right.Equals(left);
""";

        var cw = new CodeWriter();
        ValueObjectIncrementalSourceGenerator.AddEqualityOperators(cw, valueObjectTypeName, innerValueTypeName);

        NormalizeEquals(cw.ToString(), output);
    }

    [Fact]
    public void Test_AddExplicitCastOperators()
    {
        const string valueObjectTypeName = "MyValueObject";
        const string innerValueTypeName = "string";
        const string output =
$$"""
public static explicit operator {{valueObjectTypeName}}({{innerValueTypeName}} value) => From(value);
public static explicit operator {{innerValueTypeName}}({{valueObjectTypeName}} value) => value.Value;
""";

        var cw = new CodeWriter();
        ValueObjectIncrementalSourceGenerator.AddExplicitCastOperators(cw, valueObjectTypeName, innerValueTypeName);

        NormalizeEquals(cw.ToString(), output);
    }

    [Fact]
    public void Test_AddGuidSpecificCode()
    {
        const string valueObjectTypeName = "MyId";
        const string innerValueTypeName = "Guid";
        const string output =
$$"""
public static {{valueObjectTypeName}} NewId() => From(Guid.NewGuid());
""";

        var cw = new CodeWriter();
        ValueObjectIncrementalSourceGenerator.AddGuidSpecificCode(cw, valueObjectTypeName, innerValueTypeName);

        NormalizeEquals(cw.ToString(), output);
    }

    private static string Normalize(string input)
    {
        var sb = new StringBuilder(input);
        sb.Replace("    ", "\t");
        return sb.ToString().Trim();
    }

    private static void NormalizeEquals(string actual, string expected)
    {
        Normalize(actual).Should().Be(Normalize(expected));
    }
}
