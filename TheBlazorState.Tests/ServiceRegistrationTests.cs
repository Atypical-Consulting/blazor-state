using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TheBlazorState.Configuration;
using TheBlazorState.Extensions;
using TheBlazorState.Storage;
using Xunit;

namespace TheBlazorState.Tests;

public class ServiceRegistrationTests
{
    [Fact]
    public void AddTheBlazorState_Registers_Options()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTheBlazorState();

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<TheBlazorStateOptions>();
        options.ShouldNotBeNull();
    }

    [Fact]
    public void AddTheBlazorState_With_Options_Sets_DefaultStorage()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTheBlazorState(opt =>
        {
            opt.DefaultStorage = StorageStrategy.LocalStorage();
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<TheBlazorStateOptions>();
        options.DefaultStorage.ShouldNotBeNull();
    }
}
