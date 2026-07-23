using Bustand.Middleware;

namespace Bustand.Tests.TestMiddleware;

/// <summary>
/// Middleware that records all invocations for testing.
/// </summary>
public class RecordingMiddleware<TState> : IMiddleware<TState> where TState : class
{
    public List<MiddlewareContext<TState>> BeforeChangeCalls { get; } = new();
    public List<MiddlewareContext<TState>> AfterChangeCalls { get; } = new();

    public bool OnBeforeChange(MiddlewareContext<TState> context)
    {
        BeforeChangeCalls.Add(context);
        return true;
    }

    public void OnAfterChange(MiddlewareContext<TState> context)
    {
        AfterChangeCalls.Add(context);
    }
}

/// <summary>
/// Middleware that blocks state changes when ShouldBlock is true.
/// </summary>
public class BlockingMiddleware<TState> : IMiddleware<TState> where TState : class
{
    public bool ShouldBlock { get; set; }
    public int BeforeChangeCount { get; private set; }
    public int AfterChangeCount { get; private set; }

    public bool OnBeforeChange(MiddlewareContext<TState> context)
    {
        BeforeChangeCount++;
        return !ShouldBlock;
    }

    public void OnAfterChange(MiddlewareContext<TState> context)
    {
        AfterChangeCount++;
    }
}

/// <summary>
/// Middleware that throws exceptions for testing error handling.
/// </summary>
public class ThrowingMiddleware<TState> : IMiddleware<TState> where TState : class
{
    public bool ThrowOnBefore { get; set; }
    public bool ThrowOnAfter { get; set; }

    public bool OnBeforeChange(MiddlewareContext<TState> context)
    {
        if (ThrowOnBefore)
            throw new InvalidOperationException("BeforeChange error");
        return true;
    }

    public void OnAfterChange(MiddlewareContext<TState> context)
    {
        if (ThrowOnAfter)
            throw new InvalidOperationException("AfterChange error");
    }
}

/// <summary>
/// Middleware that records execution order across instances.
/// </summary>
public class OrderTrackingMiddleware<TState> : IMiddleware<TState> where TState : class
{
    private readonly int _id;
    private readonly List<string> _executionLog;

    public OrderTrackingMiddleware(int id, List<string> executionLog)
    {
        _id = id;
        _executionLog = executionLog;
    }

    public bool OnBeforeChange(MiddlewareContext<TState> context)
    {
        _executionLog.Add($"Before-{_id}");
        return true;
    }

    public void OnAfterChange(MiddlewareContext<TState> context)
    {
        _executionLog.Add($"After-{_id}");
    }
}
