using TheBlazorState.Abstractions;
using TheBlazorState.Services;
using Shouldly;
using Xunit;

namespace TheBlazorState.Generators.Tests;

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
        slice.IsStale.ShouldBeTrue();
        var result = slice.InitializeIfNeeded(99);
        result.ShouldBeTrue();
        slice.Value.ShouldBe(99);
    }

    [Fact]
    public void InitializeIfNeeded_WhenRestoredAndFresh_ReturnsFalse()
    {
        var slice = CreateSlice(42, wasRestored: true, ttl: TimeSpan.FromHours(1));
        slice.IsStale.ShouldBeFalse();
        var result = slice.InitializeIfNeeded(99);
        result.ShouldBeFalse();
        slice.Value.ShouldBe(42);
    }

    [Fact]
    public void InitializeIfNeeded_WhenNotRestored_ReturnsTrue()
    {
        var slice = CreateSlice(0, wasRestored: false);
        var result = slice.InitializeIfNeeded(99);
        result.ShouldBeTrue();
        slice.Value.ShouldBe(99);
    }

    [Fact]
    public void Value_AfterDispose_ThrowsObjectDisposedException()
    {
        var slice = CreateSlice(42, wasRestored: false);
        slice.Dispose();
        Should.Throw<ObjectDisposedException>(() => slice.Value = 99);
    }

    [Fact]
    public void Value_AfterDispose_GetStillWorks()
    {
        var slice = CreateSlice(42, wasRestored: false);
        slice.Value = 10;
        slice.Dispose();
        slice.Value.ShouldBe(10);
    }

    [Fact]
    public void Value_Set_FiresOnChanged()
    {
        var slice = CreateSlice(0, wasRestored: false);
        bool fired = false;
        slice.OnChanged += () => fired = true;
        slice.Value = 42;
        fired.ShouldBeTrue();
    }

    [Fact]
    public void Value_SetSameValue_DoesNotFireOnChanged()
    {
        var slice = CreateSlice(42, wasRestored: false);
        bool fired = false;
        slice.OnChanged += () => fired = true;
        slice.Value = 42;
        fired.ShouldBeFalse();
    }

    [Fact]
    public void IsStale_NoTTL_ReturnsFalse()
    {
        var slice = CreateSlice(42, wasRestored: true, ttl: null);
        slice.IsStale.ShouldBeFalse();
    }

    [Fact]
    public void IsStale_ZeroTTL_ReturnsTrue()
    {
        var slice = CreateSlice(42, wasRestored: true, ttl: TimeSpan.Zero);
        slice.IsStale.ShouldBeTrue();
    }

    [Fact]
    public void IsStale_LargeTTL_ReturnsFalse()
    {
        var slice = CreateSlice(42, wasRestored: true, ttl: TimeSpan.FromHours(1));
        slice.IsStale.ShouldBeFalse();
    }

    [Fact]
    public void IsDirty_InitiallyFalse()
    {
        var slice = CreateSlice(0, wasRestored: false);
        slice.IsDirty.ShouldBeFalse();
    }

    [Fact]
    public void IsDirty_TrueAfterValueSet()
    {
        var slice = CreateSlice(0, wasRestored: false);
        slice.Value = 42;
        slice.IsDirty.ShouldBeTrue();
    }

    [Fact]
    public void WasRestored_ReflectsConstructorArg()
    {
        var restored = CreateSlice(42, wasRestored: true);
        var fresh = CreateSlice(0, wasRestored: false);
        restored.WasRestored.ShouldBeTrue();
        fresh.WasRestored.ShouldBeFalse();
    }

    [Fact]
    public void Dispose_ClearsOnChanged()
    {
        var slice = CreateSlice(0, wasRestored: false);
        bool fired = false;
        slice.OnChanged += () => fired = true;
        slice.Dispose();
        // OnChanged should be null now, verify no handler remains
        fired.ShouldBeFalse();
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
        factoryCalled.ShouldBeTrue();
        slice.Value.ShouldBe(99);
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
        factoryCalled.ShouldBeFalse();
        slice.Value.ShouldBe(42);
    }

    [Fact]
    public async Task InitializeIfNeededAsync_WhenRestoredAndStale_CallsFactory()
    {
        var slice = CreateSlice(42, wasRestored: true, ttl: TimeSpan.Zero);
        await slice.InitializeIfNeededAsync(async () => 99);
        slice.Value.ShouldBe(99);
    }

    [Fact]
    public void InitializeIfNeeded_WhenNotRestored_SameValue_ReturnsFalse()
    {
        var slice = CreateSlice(42, wasRestored: false);
        var result = slice.InitializeIfNeeded(42);
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task InitializeIfNeededAsync_WhenNotRestored_SameValue_ReturnsFalse()
    {
        var slice = CreateSlice(42, wasRestored: false);
        var result = await slice.InitializeIfNeededAsync(() => Task.FromResult(42));
        result.ShouldBeFalse();
    }
}
