using Bustand.Attributes;
using Bustand.Core;
using Bustand.Extensions;
using Bustand.Tests.TestStores;
using Microsoft.Extensions.DependencyInjection;

namespace Bustand.Tests.Registration;

// Test stores for lifetime override scenarios
public record ScopedState(int Value = 0);

[BustandStore(ServiceLifetime.Scoped)]
public class ExplicitScopedStore : ZustandStore<ScopedState>
{
    public ExplicitScopedStore() : base(new ScopedState()) { }
}

public record TransientState(int Value = 0);

[BustandStore(ServiceLifetime.Transient)]
public class ExplicitTransientStore : ZustandStore<TransientState>
{
    public ExplicitTransientStore() : base(new TransientState()) { }
}

/// <summary>
/// Tests that verify ApplyPerStoreLifetimeOverrides works correctly.
/// These tests use GetRegisteredLifetime to directly inspect service descriptors.
/// </summary>
public class LifetimeOverrideTests
{
    [Fact]
    public void PerStoreOverride_SingletonAttribute_RegistersAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - default would be Scoped (Server mode simulation)
        services.AddBustand(options =>
        {
            options.ScanAssemblyContaining<SingletonStore>();
            options.DefaultLifetimeOverride = ServiceLifetime.Scoped;
        });

        // Assert - SingletonStore should override to Singleton
        var lifetime = services.GetRegisteredLifetime(typeof(SingletonStore));
        Assert.Equal(ServiceLifetime.Singleton, lifetime);
    }

    [Fact]
    public void PerStoreOverride_ScopedAttribute_RegistersAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - default would be Singleton
        services.AddBustand(options =>
        {
            options.ScanAssemblyContaining<ExplicitScopedStore>();
            options.DefaultLifetimeOverride = ServiceLifetime.Singleton;
        });

        // Assert - ExplicitScopedStore should override to Scoped
        var lifetime = services.GetRegisteredLifetime(typeof(ExplicitScopedStore));
        Assert.Equal(ServiceLifetime.Scoped, lifetime);
    }

    [Fact]
    public void PerStoreOverride_TransientAttribute_RegistersAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBustand(options =>
        {
            options.ScanAssemblyContaining<ExplicitTransientStore>();
            options.DefaultLifetimeOverride = ServiceLifetime.Singleton;
        });

        // Assert - ExplicitTransientStore should override to Transient
        var lifetime = services.GetRegisteredLifetime(typeof(ExplicitTransientStore));
        Assert.Equal(ServiceLifetime.Transient, lifetime);
    }

    [Fact]
    public void PerStoreOverride_DefaultLifetimeApplied_WhenNoAttributeLifetime()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - CounterStore has [BustandStore] with no explicit lifetime
        services.AddBustand(options =>
        {
            options.ScanAssemblyContaining<CounterStore>();
            options.DefaultLifetimeOverride = ServiceLifetime.Singleton;
        });

        // Assert - CounterStore should use default (Singleton)
        var lifetime = services.GetRegisteredLifetime(typeof(CounterStore));
        Assert.Equal(ServiceLifetime.Singleton, lifetime);
    }

    [Fact]
    public void TransientStore_ResolvesToNewInstance_EachTime()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBustand(options =>
            options.ScanAssemblyContaining<ExplicitTransientStore>());
        var provider = services.BuildServiceProvider();

        // Act
        var store1 = provider.GetRequiredService<ExplicitTransientStore>();
        var store2 = provider.GetRequiredService<ExplicitTransientStore>();

        // Assert - Transient means different instances
        Assert.NotSame(store1, store2);
    }

    [Fact]
    public void ScopedStore_SameInstanceWithinScope_DifferentAcrossScopes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBustand(options =>
            options.ScanAssemblyContaining<ExplicitScopedStore>());
        var provider = services.BuildServiceProvider();

        // Act & Assert - Same within scope
        using (var scope1 = provider.CreateScope())
        {
            var store1a = scope1.ServiceProvider.GetRequiredService<ExplicitScopedStore>();
            var store1b = scope1.ServiceProvider.GetRequiredService<ExplicitScopedStore>();
            Assert.Same(store1a, store1b);

            // Different scope = different instance
            using (var scope2 = provider.CreateScope())
            {
                var store2 = scope2.ServiceProvider.GetRequiredService<ExplicitScopedStore>();
                Assert.NotSame(store1a, store2);
            }
        }
    }
}
