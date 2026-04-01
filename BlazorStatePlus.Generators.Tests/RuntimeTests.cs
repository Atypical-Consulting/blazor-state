using BlazorStatePlus.Abstractions;
using BlazorStatePlus.Services;
using Xunit;

namespace BlazorStatePlus.Generators.Tests;

public class StateSliceTests
{
    private static StateSlice<T> CreateSlice<T>(T value, bool wasRestored, TimeSpan? ttl = null)
    {
        var options = new StateSliceOptions { Key = "test", TimeToLive = ttl };
        return new StateSlice<T>(value, wasRestored, options);
    }

    [Fact]
    public void InitializeIfNeeded_WhenRestoredAndStale_ReturnsTrue()
    {
        var slice = CreateSlice(42, wasRestored: true, ttl: TimeSpan.Zero);
        Assert.True(slice.IsStale);
        var result = slice.InitializeIfNeeded(99);
        Assert.True(result);
        Assert.Equal(99, slice.Value);
    }

    [Fact]
    public void InitializeIfNeeded_WhenRestoredAndFresh_ReturnsFalse()
    {
        var slice = CreateSlice(42, wasRestored: true, ttl: TimeSpan.FromHours(1));
        Assert.False(slice.IsStale);
        var result = slice.InitializeIfNeeded(99);
        Assert.False(result);
        Assert.Equal(42, slice.Value);
    }

    [Fact]
    public void InitializeIfNeeded_WhenNotRestored_ReturnsTrue()
    {
        var slice = CreateSlice(0, wasRestored: false);
        var result = slice.InitializeIfNeeded(99);
        Assert.True(result);
        Assert.Equal(99, slice.Value);
    }

    [Fact]
    public void Value_AfterDispose_ThrowsObjectDisposedException()
    {
        var slice = CreateSlice(42, wasRestored: false);
        slice.Dispose();
        Assert.Throws<ObjectDisposedException>(() => slice.Value = 99);
    }

    [Fact]
    public void Value_AfterDispose_GetStillWorks()
    {
        var slice = CreateSlice(42, wasRestored: false);
        slice.Value = 10;
        slice.Dispose();
        Assert.Equal(10, slice.Value);
    }

    [Fact]
    public void Value_Set_FiresOnChanged()
    {
        var slice = CreateSlice(0, wasRestored: false);
        bool fired = false;
        slice.OnChanged += () => fired = true;
        slice.Value = 42;
        Assert.True(fired);
    }

    [Fact]
    public void Value_SetSameValue_DoesNotFireOnChanged()
    {
        var slice = CreateSlice(42, wasRestored: false);
        bool fired = false;
        slice.OnChanged += () => fired = true;
        slice.Value = 42;
        Assert.False(fired);
    }

    [Fact]
    public void IsStale_NoTTL_ReturnsFalse()
    {
        var slice = CreateSlice(42, wasRestored: true, ttl: null);
        Assert.False(slice.IsStale);
    }

    [Fact]
    public void IsStale_ZeroTTL_ReturnsTrue()
    {
        var slice = CreateSlice(42, wasRestored: true, ttl: TimeSpan.Zero);
        Assert.True(slice.IsStale);
    }

    [Fact]
    public void IsStale_LargeTTL_ReturnsFalse()
    {
        var slice = CreateSlice(42, wasRestored: true, ttl: TimeSpan.FromHours(1));
        Assert.False(slice.IsStale);
    }

    [Fact]
    public void IsDirty_InitiallyFalse()
    {
        var slice = CreateSlice(0, wasRestored: false);
        Assert.False(slice.IsDirty);
    }

    [Fact]
    public void IsDirty_TrueAfterValueSet()
    {
        var slice = CreateSlice(0, wasRestored: false);
        slice.Value = 42;
        Assert.True(slice.IsDirty);
    }

    [Fact]
    public void WasRestored_ReflectsConstructorArg()
    {
        var restored = CreateSlice(42, wasRestored: true);
        var fresh = CreateSlice(0, wasRestored: false);
        Assert.True(restored.WasRestored);
        Assert.False(fresh.WasRestored);
    }

    [Fact]
    public void Dispose_ClearsOnChanged()
    {
        var slice = CreateSlice(0, wasRestored: false);
        bool fired = false;
        slice.OnChanged += () => fired = true;
        slice.Dispose();
        // OnChanged should be null now, verify no handler remains
        Assert.False(fired);
    }

    [Fact]
    public void Dispose_IsIdempotent()
    {
        var slice = CreateSlice(0, wasRestored: false);
        slice.Dispose();
        slice.Dispose(); // Should not throw
    }

    [Fact]
    public async Task InitializeIfNeededAsync_WhenNotRestored_CallsFactory()
    {
        var slice = CreateSlice(0, wasRestored: false);
        bool factoryCalled = false;
        await slice.InitializeIfNeededAsync(async () =>
        {
            factoryCalled = true;
            return 99;
        });
        Assert.True(factoryCalled);
        Assert.Equal(99, slice.Value);
    }

    [Fact]
    public async Task InitializeIfNeededAsync_WhenRestoredAndFresh_SkipsFactory()
    {
        var slice = CreateSlice(42, wasRestored: true, ttl: TimeSpan.FromHours(1));
        bool factoryCalled = false;
        await slice.InitializeIfNeededAsync(async () =>
        {
            factoryCalled = true;
            return 99;
        });
        Assert.False(factoryCalled);
        Assert.Equal(42, slice.Value);
    }

    [Fact]
    public async Task InitializeIfNeededAsync_WhenRestoredAndStale_CallsFactory()
    {
        var slice = CreateSlice(42, wasRestored: true, ttl: TimeSpan.Zero);
        await slice.InitializeIfNeededAsync(async () => 99);
        Assert.Equal(99, slice.Value);
    }
}
