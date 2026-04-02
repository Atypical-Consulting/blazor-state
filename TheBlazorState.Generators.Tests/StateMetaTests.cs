using Shouldly;
using TheBlazorState.Abstractions;
using Xunit;

namespace TheBlazorState.Generators.Tests;

public class StateMetaTests
{
    [Fact]
    public void StateMeta_Defaults()
    {
        var meta = new StateMeta(ttl: null);
        meta.WasRestored.ShouldBeFalse();
        meta.IsDirty.ShouldBeFalse();
        meta.IsStale.ShouldBeFalse();
        meta.LastUpdated.ShouldBeGreaterThan(DateTimeOffset.MinValue);
    }

    [Fact]
    public void StateMeta_WasRestored_When_Set()
    {
        var meta = new StateMeta(ttl: null);
        meta.MarkRestored(DateTimeOffset.UtcNow.AddMinutes(-1));
        meta.WasRestored.ShouldBeTrue();
    }

    [Fact]
    public void StateMeta_IsDirty_After_MarkDirty()
    {
        var meta = new StateMeta(ttl: null);
        meta.MarkDirty();
        meta.IsDirty.ShouldBeTrue();
    }

    [Fact]
    public void StateMeta_IsStale_When_TTL_Exceeded()
    {
        var meta = new StateMeta(ttl: TimeSpan.FromMinutes(5));
        meta.MarkRestored(DateTimeOffset.UtcNow.AddMinutes(-10));
        meta.IsStale.ShouldBeTrue();
    }

    [Fact]
    public void StateMeta_Not_Stale_When_TTL_Fresh()
    {
        var meta = new StateMeta(ttl: TimeSpan.FromMinutes(5));
        meta.MarkRestored(DateTimeOffset.UtcNow.AddMinutes(-1));
        meta.IsStale.ShouldBeFalse();
    }

    [Fact]
    public void StateMeta_Not_Stale_Without_TTL()
    {
        var meta = new StateMeta(ttl: null);
        meta.MarkRestored(DateTimeOffset.UtcNow.AddHours(-24));
        meta.IsStale.ShouldBeFalse();
    }

    [Fact]
    public void StateMeta_OnChanged_Fires()
    {
        var meta = new StateMeta(ttl: null);
        bool fired = false;
        meta.OnChanged += () => fired = true;
        meta.RaiseChanged();
        fired.ShouldBeTrue();
    }
}
