using Shouldly;
using TheBlazorState.Storage;
using Xunit;

namespace TheBlazorState.Tests;

public class StorageStrategyTests
{
    [Fact]
    public void StorageResult_Found_Contains_Value()
    {
        var result = new StorageResult<string>(true, "hello", DateTimeOffset.UtcNow);
        result.Found.ShouldBeTrue();
        result.Value.ShouldBe("hello");
        result.PersistedAt.ShouldNotBeNull();
    }

    [Fact]
    public void StorageResult_NotFound()
    {
        var result = new StorageResult<string>(false, null, null);
        result.Found.ShouldBeFalse();
        result.Value.ShouldBeNull();
    }

    [Fact]
    public void StorageMetadata_Captures_Key_And_TTL()
    {
        var meta = new StorageMetadata("MyComponent.Product", TimeSpan.FromMinutes(5), DateTimeOffset.UtcNow);
        meta.Key.ShouldBe("MyComponent.Product");
        meta.TimeToLive.ShouldBe(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void StorageStrategy_PrerenderHtml_Returns_Instance()
    {
        var strategy = StorageStrategy.PrerenderHtml();
        strategy.ShouldNotBeNull();
    }

    [Fact]
    public void StorageStrategy_SessionStorage_Returns_Instance()
    {
        var strategy = StorageStrategy.SessionStorage();
        strategy.ShouldNotBeNull();
    }

    [Fact]
    public void StorageStrategy_LocalStorage_Returns_Instance()
    {
        var strategy = StorageStrategy.LocalStorage();
        strategy.ShouldNotBeNull();
    }

    [Fact]
    public void StorageStrategy_IndexedDb_Returns_Instance()
    {
        var strategy = StorageStrategy.IndexedDb();
        strategy.ShouldNotBeNull();
    }

    [Fact]
    public void StorageStrategy_ServerMemoryCache_Returns_Instance()
    {
        var strategy = StorageStrategy.ServerMemoryCache();
        strategy.ShouldNotBeNull();
    }
}
