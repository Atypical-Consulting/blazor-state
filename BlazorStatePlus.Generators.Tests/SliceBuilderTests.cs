using BlazorStatePlus.Abstractions;
using Shouldly;
using Xunit;

namespace BlazorStatePlus.Generators.Tests;

public class SliceBuilderTests
{
    [Fact]
    public void ResolveKey_NoOverride_ReturnsBaseKey()
    {
        var builder = new SliceBuilder<int>();
        builder.ResolveKey("Component.counter").ShouldBe("Component.counter");
    }

    [Fact]
    public void ResolveKey_WithSuffix_AppendsSeparatedByColon()
    {
        var builder = new SliceBuilder<int>();
        builder.KeySuffix(42);
        builder.ResolveKey("Component.counter").ShouldBe("Component.counter:42");
    }

    [Fact]
    public void ResolveKey_WithMultipleSuffixes_JoinsWithColon()
    {
        var builder = new SliceBuilder<string>();
        builder.KeySuffix(1, "en");
        builder.ResolveKey("Component.name").ShouldBe("Component.name:1:en");
    }

    [Fact]
    public void ResolveKey_WithOverride_IgnoresBaseKey()
    {
        var builder = new SliceBuilder<int>();
        builder.KeyOverride("custom-key");
        builder.ResolveKey("Component.counter").ShouldBe("custom-key");
    }

    [Fact]
    public void GetDefaultValue_ReturnsDefault_WhenNotSet()
    {
        var builder = new SliceBuilder<int>();
        builder.GetDefaultValue().ShouldBe(0);
    }

    [Fact]
    public void GetDefaultValue_ReturnsSetValue()
    {
        var builder = new SliceBuilder<int>();
        builder.DefaultValue(42);
        builder.GetDefaultValue().ShouldBe(42);
    }

    [Fact]
    public void HasAsyncFactory_FalseByDefault()
    {
        var builder = new SliceBuilder<int>();
        builder.HasAsyncFactory.ShouldBeFalse();
    }

    [Fact]
    public void HasAsyncFactory_TrueAfterInitializeFrom()
    {
        var builder = new SliceBuilder<int>();
        builder.InitializeFrom(() => Task.FromResult(99));
        builder.HasAsyncFactory.ShouldBeTrue();
    }

    [Fact]
    public void FluentApi_ReturnsSameInstance()
    {
        var builder = new SliceBuilder<int>();
        var same = builder.KeySuffix(1).DefaultValue(0).KeyOverride("x");
        same.ShouldBeSameAs(builder);
    }
}
