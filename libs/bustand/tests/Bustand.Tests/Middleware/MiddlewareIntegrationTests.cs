using Bustand.Extensions;
using Bustand.Tests.TestMiddleware;
using Bustand.Tests.TestStores;
using Microsoft.Extensions.DependencyInjection;

namespace Bustand.Tests.Middleware;

public class MiddlewareIntegrationTests
{
    [Fact]
    public void Store_WithMiddleware_ReceivesPipeline()
    {
        var services = new ServiceCollection();
        var recording = new RecordingMiddleware<CounterState>();

        services.AddSingleton(recording);
        services.AddBustand(options =>
        {
            options.ScanAssemblyContaining<CounterStore>();
            options.UseMiddleware<RecordingMiddleware<CounterState>>();
        });

        var sp = services.BuildServiceProvider();
        var store = sp.GetRequiredService<CounterStore>();

        store.Increment();

        Assert.Single(recording.AfterChangeCalls);
        Assert.Equal(1, recording.AfterChangeCalls[0].NewState.Count);
    }

    [Fact]
    public void Store_WithBlockingMiddleware_BlocksStateChange()
    {
        var services = new ServiceCollection();
        var blocking = new BlockingMiddleware<CounterState> { ShouldBlock = true };

        services.AddSingleton(blocking);
        services.AddBustand(options =>
        {
            options.ScanAssemblyContaining<CounterStore>();
            options.UseMiddleware<BlockingMiddleware<CounterState>>();
        });

        var sp = services.BuildServiceProvider();
        var store = sp.GetRequiredService<CounterStore>();
        var initialState = store.State;

        store.Increment();

        Assert.Equal(initialState.Count, store.State.Count); // State unchanged
        Assert.Equal(1, blocking.BeforeChangeCount);
        Assert.Equal(0, blocking.AfterChangeCount); // AfterChange not called when blocked
    }

    [Fact]
    public void Store_WithoutMiddleware_WorksNormally()
    {
        var services = new ServiceCollection();
        services.AddBustand(options =>
        {
            options.ScanAssemblyContaining<CounterStore>();
            // No middleware registered
        });

        var sp = services.BuildServiceProvider();
        var store = sp.GetRequiredService<CounterStore>();

        store.Increment();
        store.Increment();

        Assert.Equal(2, store.State.Count);
    }

    [Fact]
    public void Store_CapturesActionName()
    {
        var services = new ServiceCollection();
        var recording = new RecordingMiddleware<CounterState>();

        services.AddSingleton(recording);
        services.AddBustand(options =>
        {
            options.ScanAssemblyContaining<CounterStore>();
            options.UseMiddleware<RecordingMiddleware<CounterState>>();
        });

        var sp = services.BuildServiceProvider();
        var store = sp.GetRequiredService<CounterStore>();

        store.Increment();

        // ActionName should be captured via [CallerMemberName]
        Assert.Equal("Increment", recording.AfterChangeCalls[0].ActionName);
    }
}
