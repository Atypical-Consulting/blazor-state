using Bustand.Middleware;
using Bustand.Tests.TestMiddleware;
using Bustand.Tests.TestStores;

namespace Bustand.Tests.Middleware;

public class MiddlewarePipelineTests
{
    private static MiddlewareContext<CounterState> CreateContext(int oldCount = 0, int newCount = 1) =>
        new()
        {
            OldState = new CounterState(oldCount),
            NewState = new CounterState(newCount),
            StoreType = typeof(CounterStore),
            ActionName = "TestAction",
            Timestamp = DateTimeOffset.UtcNow
        };

    [Fact]
    public void Empty_InvokeBeforeChange_ReturnsTrue()
    {
        var pipeline = MiddlewarePipeline<CounterState>.Empty;
        var context = CreateContext();

        var result = pipeline.InvokeBeforeChange(context);

        Assert.True(result);
    }

    [Fact]
    public void Empty_InvokeAfterChange_DoesNotThrow()
    {
        var pipeline = MiddlewarePipeline<CounterState>.Empty;
        var context = CreateContext();

        var exception = Record.Exception(() => pipeline.InvokeAfterChange(context));

        Assert.Null(exception);
    }

    [Fact]
    public void InvokeBeforeChange_AllReturnTrue_ReturnsTrue()
    {
        var mw1 = new RecordingMiddleware<CounterState>();
        var mw2 = new RecordingMiddleware<CounterState>();
        var pipeline = new MiddlewarePipeline<CounterState>(new[] { mw1, mw2 });
        var context = CreateContext();

        var result = pipeline.InvokeBeforeChange(context);

        Assert.True(result);
        Assert.Single(mw1.BeforeChangeCalls);
        Assert.Single(mw2.BeforeChangeCalls);
    }

    [Fact]
    public void InvokeBeforeChange_OneReturnsFalse_BlocksAndStopsIteration()
    {
        var mw1 = new BlockingMiddleware<CounterState> { ShouldBlock = true };
        var mw2 = new RecordingMiddleware<CounterState>();
        var pipeline = new MiddlewarePipeline<CounterState>(new IMiddleware<CounterState>[] { mw1, mw2 });
        var context = CreateContext();

        var result = pipeline.InvokeBeforeChange(context);

        Assert.False(result);
        Assert.Equal(1, mw1.BeforeChangeCount);
        Assert.Empty(mw2.BeforeChangeCalls); // Second middleware not called
    }

    [Fact]
    public void InvokeBeforeChange_ExecutesInRegistrationOrder()
    {
        var log = new List<string>();
        var mw1 = new OrderTrackingMiddleware<CounterState>(1, log);
        var mw2 = new OrderTrackingMiddleware<CounterState>(2, log);
        var mw3 = new OrderTrackingMiddleware<CounterState>(3, log);
        var pipeline = new MiddlewarePipeline<CounterState>(new[] { mw1, mw2, mw3 });
        var context = CreateContext();

        pipeline.InvokeBeforeChange(context);

        Assert.Equal(new[] { "Before-1", "Before-2", "Before-3" }, log);
    }

    [Fact]
    public void InvokeAfterChange_ExecutesInRegistrationOrder()
    {
        var log = new List<string>();
        var mw1 = new OrderTrackingMiddleware<CounterState>(1, log);
        var mw2 = new OrderTrackingMiddleware<CounterState>(2, log);
        var pipeline = new MiddlewarePipeline<CounterState>(new[] { mw1, mw2 });
        var context = CreateContext();

        pipeline.InvokeAfterChange(context);

        Assert.Equal(new[] { "After-1", "After-2" }, log);
    }

    [Fact]
    public void InvokeAfterChange_ExceptionInMiddleware_ContinuesToNext()
    {
        var throwing = new ThrowingMiddleware<CounterState> { ThrowOnAfter = true };
        var recording = new RecordingMiddleware<CounterState>();
        var pipeline = new MiddlewarePipeline<CounterState>(new IMiddleware<CounterState>[] { throwing, recording });
        var context = CreateContext();

        // Should not throw, should continue to next middleware
        var exception = Record.Exception(() => pipeline.InvokeAfterChange(context));

        Assert.Null(exception);
        Assert.Single(recording.AfterChangeCalls); // Second middleware was called
    }

    [Fact]
    public void InvokeBeforeChange_ExceptionInMiddleware_BubblesUp()
    {
        var throwing = new ThrowingMiddleware<CounterState> { ThrowOnBefore = true };
        var pipeline = new MiddlewarePipeline<CounterState>(new[] { throwing });
        var context = CreateContext();

        Assert.Throws<InvalidOperationException>(() => pipeline.InvokeBeforeChange(context));
    }

    [Fact]
    public void Context_ContainsAllExpectedData()
    {
        var recording = new RecordingMiddleware<CounterState>();
        var pipeline = new MiddlewarePipeline<CounterState>(new[] { recording });
        var context = CreateContext(5, 10);

        pipeline.InvokeBeforeChange(context);

        var captured = recording.BeforeChangeCalls.Single();
        Assert.Equal(5, captured.OldState.Count);
        Assert.Equal(10, captured.NewState.Count);
        Assert.Equal(typeof(CounterStore), captured.StoreType);
        Assert.Equal("TestAction", captured.ActionName);
        Assert.True(captured.Timestamp <= DateTimeOffset.UtcNow);
    }
}
