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
}
