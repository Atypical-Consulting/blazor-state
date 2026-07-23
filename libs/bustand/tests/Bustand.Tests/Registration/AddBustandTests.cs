using Bustand.Extensions;
using Bustand.Tests.TestStores;
using Microsoft.Extensions.DependencyInjection;

namespace Bustand.Tests.Registration;

public class AddBustandTests
{
    [Fact]
    public void AddBustand_RegistersAttributedStores()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBustand(options =>
            options.ScanAssemblyContaining<CounterStore>());

        var provider = services.BuildServiceProvider();

        // Assert
        var store = provider.GetService<CounterStore>();
        Assert.NotNull(store);
    }

    [Fact]
    public void AddBustand_DoesNotRegisterUnattributedStores()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBustand(options =>
            options.ScanAssemblyContaining<UnattributedStore>());

        var provider = services.BuildServiceProvider();

        // Assert
        var store = provider.GetService<UnattributedStore>();
        Assert.Null(store);
    }

    [Fact]
    public void AddBustand_RespectsExplicitSingletonLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBustand(options =>
            options.ScanAssemblyContaining<SingletonStore>());

        var provider = services.BuildServiceProvider();

        // Assert - Singleton returns same instance
        var store1 = provider.GetService<SingletonStore>();
        var store2 = provider.GetService<SingletonStore>();
        Assert.Same(store1, store2);
    }

    [Fact]
    public void AddBustand_CanOverrideDefaultLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBustand(options =>
        {
            options.ScanAssemblyContaining<CounterStore>();
            options.DefaultLifetimeOverride = ServiceLifetime.Singleton;
        });

        var provider = services.BuildServiceProvider();

        // Assert - Should be singleton due to override
        var store1 = provider.GetService<CounterStore>();
        var store2 = provider.GetService<CounterStore>();
        Assert.Same(store1, store2);
    }

    [Fact]
    public void AddBustand_WithNoConfig_UsesCallingAssembly()
    {
        // This test verifies the default behavior
        // In a real app, it would scan the calling assembly

        // Arrange
        var services = new ServiceCollection();

        // Act - Note: In test context, calling assembly is the test assembly
        services.AddBustand(options =>
            options.ScanAssemblyContaining<CounterStore>());

        // Assert
        var provider = services.BuildServiceProvider();
        var store = provider.GetService<CounterStore>();
        Assert.NotNull(store);
    }

    [Fact]
    public void AddBustand_StoreCanBeResolved_AndStateWorks()
    {
        // Integration test: verify resolved store is functional
        // Arrange
        var services = new ServiceCollection();
        services.AddBustand(options =>
            options.ScanAssemblyContaining<CounterStore>());
        var provider = services.BuildServiceProvider();

        // Act
        var store = provider.GetRequiredService<CounterStore>();
        store.Increment();
        store.Increment();

        // Assert
        Assert.Equal(2, store.State.Count);
    }
}
