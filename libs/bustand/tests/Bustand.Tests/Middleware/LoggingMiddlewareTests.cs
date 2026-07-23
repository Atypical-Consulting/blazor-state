using Bustand.Middleware;
using Bustand.Tests.TestStores;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Bustand.Tests.Middleware;

public class LoggingMiddlewareTests
{
    private readonly ILogger<LoggingMiddleware<CounterState>> _logger;
    private readonly LoggingMiddleware<CounterState> _middleware;

    public LoggingMiddlewareTests()
    {
        _logger = Substitute.For<ILogger<LoggingMiddleware<CounterState>>>();
        _middleware = new LoggingMiddleware<CounterState>(_logger);
    }

    [Fact]
    public void OnBeforeChange_AlwaysReturnsTrue()
    {
        var context = new MiddlewareContext<CounterState>
        {
            OldState = new CounterState(0),
            NewState = new CounterState(1),
            StoreType = typeof(CounterStore),
            Timestamp = DateTimeOffset.UtcNow
        };

        var result = _middleware.OnBeforeChange(context);

        Assert.True(result);
    }

    [Fact]
    public void OnAfterChange_LoggingDisabled_DoesNotLog()
    {
        _logger.IsEnabled(LogLevel.Debug).Returns(false);
        var context = new MiddlewareContext<CounterState>
        {
            OldState = new CounterState(0),
            NewState = new CounterState(1),
            StoreType = typeof(CounterStore),
            Timestamp = DateTimeOffset.UtcNow
        };

        _middleware.OnAfterChange(context);

        // No log calls when disabled
        _logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void OnAfterChange_StoreExcluded_DoesNotLog()
    {
        var options = new LoggingMiddlewareOptions
        {
            ExcludeStores = new HashSet<Type> { typeof(CounterStore) }
        };
        var middleware = new LoggingMiddleware<CounterState>(
            _logger,
            Options.Create(options));
        _logger.IsEnabled(LogLevel.Debug).Returns(true);

        var context = new MiddlewareContext<CounterState>
        {
            OldState = new CounterState(0),
            NewState = new CounterState(1),
            StoreType = typeof(CounterStore),
            Timestamp = DateTimeOffset.UtcNow
        };

        middleware.OnAfterChange(context);

        _logger.DidNotReceive().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void OnAfterChange_StateChanged_Logs()
    {
        _logger.IsEnabled(LogLevel.Debug).Returns(true);
        var context = new MiddlewareContext<CounterState>
        {
            OldState = new CounterState(0),
            NewState = new CounterState(5),
            StoreType = typeof(CounterStore),
            ActionName = "Increment",
            Timestamp = DateTimeOffset.UtcNow
        };

        _middleware.OnAfterChange(context);

        // Verify logger was called at Debug level
        _logger.Received().Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void OnAfterChange_NoChange_DoesNotLog()
    {
        _logger.IsEnabled(LogLevel.Debug).Returns(true);
        var state = new CounterState(5);
        var context = new MiddlewareContext<CounterState>
        {
            OldState = state,
            NewState = state, // Same reference
            StoreType = typeof(CounterStore),
            Timestamp = DateTimeOffset.UtcNow
        };

        _middleware.OnAfterChange(context);

        // No log when states are equal
        _logger.DidNotReceive().Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
