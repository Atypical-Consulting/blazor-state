using Microsoft.Extensions.Caching.Memory;
using Shouldly;
using TheBlazorState.Configuration;
using TheBlazorState.Storage;
using Xunit;

namespace TheBlazorState.Tests;

public class LibraryBugTests
{
    [Fact]
    public void PropertyConfigurator_KeySuffix_With_Null_Throws()
    {
        var config = new PropertyConfigurator<string>();
        Should.Throw<ArgumentNullException>(() => config.KeySuffix(null!));
    }

    [Fact]
    public void PropertyConfigurator_KeySuffix_With_Null_Element_Throws()
    {
        var config = new PropertyConfigurator<string>();
        Should.Throw<ArgumentNullException>(() => config.KeySuffix("a", null!, "b"));
    }

    [Fact]
    public void PropertyConfigurator_KeyOverride_With_Null_Throws()
    {
        var config = new PropertyConfigurator<string>();
        Should.Throw<ArgumentNullException>(() => config.KeyOverride(null!));
    }

    [Fact]
    public void PropertyConfigurator_KeyOverride_With_Empty_Throws()
    {
        var config = new PropertyConfigurator<string>();
        Should.Throw<ArgumentException>(() => config.KeyOverride(""));
    }

    [Fact]
    public async Task ServerMemoryCacheStrategy_RestoreAsync_Wrong_Type_Returns_NotFound()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var strategy = new ServerMemoryCacheStrategy();
        strategy.Initialize(cache);

        // Persist as string
        await strategy.PersistAsync("key", "hello", new StorageMetadata("key", null, DateTimeOffset.UtcNow));

        // Try to restore as int — should not crash
        var result = await strategy.RestoreAsync<int>("key");
        result.Found.ShouldBeFalse();
    }

    [Fact]
    public async Task PropertyConfigurator_InvokeFactoryAsync_Without_Factory_Throws()
    {
        var config = new PropertyConfigurator<string>();
        await Should.ThrowAsync<InvalidOperationException>(config.InvokeFactoryAsync);
    }
}
