using BlazorStatePlus.Abstractions;
using Xunit;

namespace BlazorStatePlus.Generators.Tests;

public class SliceBuilderTests
{
    [Fact]
    public void ResolveKey_NoOverride_ReturnsBaseKey()
    {
        var builder = new SliceBuilder<int>();
        Assert.Equal("Component.counter", builder.ResolveKey("Component.counter"));
    }

    [Fact]
    public void ResolveKey_WithSuffix_AppendsSeparatedByColon()
    {
        var builder = new SliceBuilder<int>();
        builder.KeySuffix(42);
        Assert.Equal("Component.counter:42", builder.ResolveKey("Component.counter"));
    }

    [Fact]
    public void ResolveKey_WithMultipleSuffixes_JoinsWithColon()
    {
        var builder = new SliceBuilder<string>();
        builder.KeySuffix(1, "en");
        Assert.Equal("Component.name:1:en", builder.ResolveKey("Component.name"));
    }

    [Fact]
    public void ResolveKey_WithOverride_IgnoresBaseKey()
    {
        var builder = new SliceBuilder<int>();
        builder.KeyOverride("custom-key");
        Assert.Equal("custom-key", builder.ResolveKey("Component.counter"));
    }

    [Fact]
    public void GetDefaultValue_ReturnsDefault_WhenNotSet()
    {
        var builder = new SliceBuilder<int>();
        Assert.Equal(0, builder.GetDefaultValue());
    }

    [Fact]
    public void GetDefaultValue_ReturnsSetValue()
    {
        var builder = new SliceBuilder<int>();
        builder.DefaultValue(42);
        Assert.Equal(42, builder.GetDefaultValue());
    }

    [Fact]
    public void HasAsyncFactory_FalseByDefault()
    {
        var builder = new SliceBuilder<int>();
        Assert.False(builder.HasAsyncFactory);
    }

    [Fact]
    public void HasAsyncFactory_TrueAfterInitializeFrom()
    {
        var builder = new SliceBuilder<int>();
        builder.InitializeFrom(() => Task.FromResult(99));
        Assert.True(builder.HasAsyncFactory);
    }

    [Fact]
    public void BuildOptions_AppliesAttributeDefaults()
    {
        var builder = new SliceBuilder<int>();
        var options = new StateSliceOptions();

        var configure = builder.BuildOptions(o => o.TimeToLive = TimeSpan.FromMinutes(5));
        configure?.Invoke(options);

        Assert.Equal(TimeSpan.FromMinutes(5), options.TimeToLive);
    }

    [Fact]
    public void FluentApi_ReturnsSameInstance()
    {
        var builder = new SliceBuilder<int>();
        var same = builder.KeySuffix(1).DefaultValue(0).KeyOverride("x");
        Assert.Same(builder, same);
    }
}
