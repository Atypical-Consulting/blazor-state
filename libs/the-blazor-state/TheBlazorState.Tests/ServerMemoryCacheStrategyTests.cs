using Microsoft.Extensions.Caching.Memory;
using Shouldly;
using TheBlazorState.Storage;
using Xunit;

namespace TheBlazorState.Tests;

public class ServerMemoryCacheStrategyTests
{
    private static ServerMemoryCacheStrategy CreateStrategy()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var strategy = new ServerMemoryCacheStrategy();
        strategy.Initialize(cache);
        return strategy;
    }

    [Fact]
    public async Task PersistAndRestore_RoundTrips()
    {
        var strategy = CreateStrategy();
        var metadata = new StorageMetadata("test.key", null, DateTimeOffset.UtcNow);
        await strategy.PersistAsync("test.key", "hello", metadata);

        var result = await strategy.RestoreAsync<string>("test.key");
        result.Found.ShouldBeTrue();
        result.Value.ShouldBe("hello");
        result.PersistedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task RestoreAsync_Returns_NotFound_When_Empty()
    {
        var strategy = CreateStrategy();
        var result = await strategy.RestoreAsync<string>("nonexistent");
        result.Found.ShouldBeFalse();
    }

    [Fact]
    public async Task RemoveAsync_Clears_Value()
    {
        var strategy = CreateStrategy();
        var metadata = new StorageMetadata("test.key", null, DateTimeOffset.UtcNow);
        await strategy.PersistAsync("test.key", 42, metadata);
        await strategy.RemoveAsync("test.key");

        var result = await strategy.RestoreAsync<int>("test.key");
        result.Found.ShouldBeFalse();
    }

    [Fact]
    public async Task PersistAsync_Preserves_Timestamp()
    {
        var strategy = CreateStrategy();
        var timestamp = DateTimeOffset.UtcNow.AddMinutes(-5);
        var metadata = new StorageMetadata("test.key", null, timestamp);
        await strategy.PersistAsync("test.key", "data", metadata);

        var result = await strategy.RestoreAsync<string>("test.key");
        result.PersistedAt.ShouldBe(timestamp);
    }

    [Fact]
    public async Task PersistAsync_Overwrites_Previous()
    {
        var strategy = CreateStrategy();
        var metadata = new StorageMetadata("test.key", null, DateTimeOffset.UtcNow);
        await strategy.PersistAsync("test.key", "first", metadata);
        await strategy.PersistAsync("test.key", "second", metadata);

        var result = await strategy.RestoreAsync<string>("test.key");
        result.Value.ShouldBe("second");
    }

    [Fact]
    public async Task Works_With_Complex_Types()
    {
        var strategy = CreateStrategy();
        var data = new TestRecord("hello", 42, true);
        var metadata = new StorageMetadata("test.key", null, DateTimeOffset.UtcNow);
        await strategy.PersistAsync("test.key", data, metadata);

        var result = await strategy.RestoreAsync<TestRecord>("test.key");
        result.Found.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("hello");
        result.Value.Count.ShouldBe(42);
        result.Value.Active.ShouldBeTrue();
    }

    private record TestRecord(string Name, int Count, bool Active);
}
