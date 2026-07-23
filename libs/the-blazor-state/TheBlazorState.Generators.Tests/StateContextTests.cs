using Shouldly;
using TheBlazorState.Configuration;
using TheBlazorState.Storage;
using Xunit;

namespace TheBlazorState.Generators.Tests;

public class StateContextTests
{
    [Fact]
    public void PropertyConfigurator_KeySuffix_Appends_To_BaseKey()
    {
        var config = new PropertyConfigurator<string>();
        config.KeySuffix(42);
        config.ResolveKey("Component.Name").ShouldBe("Component.Name:42");
    }

    [Fact]
    public void PropertyConfigurator_KeySuffix_Multiple_Parts()
    {
        var config = new PropertyConfigurator<string>();
        config.KeySuffix("us", 42);
        config.ResolveKey("Component.Name").ShouldBe("Component.Name:us:42");
    }

    [Fact]
    public void PropertyConfigurator_KeyOverride_Replaces_BaseKey()
    {
        var config = new PropertyConfigurator<string>();
        config.KeyOverride("custom-key");
        config.ResolveKey("Component.Name").ShouldBe("custom-key");
    }

    [Fact]
    public void PropertyConfigurator_LoadFrom_Sets_Factory()
    {
        var config = new PropertyConfigurator<string>();
        config.LoadFrom(() => Task.FromResult("loaded"));
        config.HasAsyncFactory.ShouldBeTrue();
    }

    [Fact]
    public void PropertyConfigurator_No_Factory_By_Default()
    {
        var config = new PropertyConfigurator<string>();
        config.HasAsyncFactory.ShouldBeFalse();
    }

    [Fact]
    public void PropertyConfigurator_Storage_Can_Be_Set()
    {
        var config = new PropertyConfigurator<string>();
        config.Storage = StorageStrategy.SessionStorage();
        config.Storage.ShouldNotBeNull();
    }

    [Fact]
    public void PropertyConfigurator_Storage_Null_By_Default()
    {
        var config = new PropertyConfigurator<string>();
        config.Storage.ShouldBeNull();
    }

    [Fact]
    public void StateContext_Storage_Can_Be_Set_At_Component_Level()
    {
        var ctx = new StateContext();
        ctx.Storage = StorageStrategy.LocalStorage();
        ctx.Storage.ShouldNotBeNull();
    }
}
