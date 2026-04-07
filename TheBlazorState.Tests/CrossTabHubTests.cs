using Shouldly;
using TheBlazorState.Storage;
using Xunit;

namespace TheBlazorState.Tests;

/// <summary>
/// Tests for the server-side cross-circuit notification hub.
/// CrossTabHub is a singleton that allows circuits to publish/subscribe
/// to value changes, enabling cross-tab sync without any JS callbacks.
///
/// Note: Publish dispatches callbacks asynchronously via Task.Run,
/// so tests use SemaphoreSlim to wait for delivery.
/// </summary>
public class CrossTabHubTests : IDisposable
{
    private readonly CrossTabHub _hub = new();

    public void Dispose() => _hub.Dispose();

    private static async Task WaitFor(SemaphoreSlim signal, int timeoutMs = 1000)
    {
        (await signal.WaitAsync(timeoutMs)).ShouldBeTrue("Timed out waiting for hub notification");
    }

    // ---------------------------------------------------------------
    // Subscribe + Publish: other subscribers receive the value
    // ---------------------------------------------------------------

    [Fact]
    public async Task Publish_NotifiesOtherSubscribers()
    {
        string? receivedJson = null;
        string? receivedKey = null;
        var signal = new SemaphoreSlim(0, 1);

        var subA = _hub.Subscribe("Test.Counter", (key, json) =>
        {
            receivedKey = key;
            receivedJson = json;
            signal.Release();
        });

        _hub.Publish("Test.Counter", """{"value":42}""", publisherId: "circuitB");
        await WaitFor(signal);

        receivedKey.ShouldBe("Test.Counter");
        receivedJson.ShouldBe("""{"value":42}""");

        subA.Dispose();
    }

    [Fact]
    public async Task Publish_DoesNotNotifyPublisher()
    {
        bool called = false;

        var subA = _hub.Subscribe("Test.Counter", (_, _) => called = true, subscriberId: "circuitA");

        _hub.Publish("Test.Counter", """{"value":42}""", publisherId: "circuitA");
        await Task.Delay(100); // give time for async dispatch if it were to fire

        called.ShouldBeFalse();

        subA.Dispose();
    }

    [Fact]
    public async Task Publish_NotifiesMultipleSubscribers()
    {
        int callCount = 0;
        var signal = new SemaphoreSlim(0, 3);

        var sub1 = _hub.Subscribe("Test.Counter", (_, _) => { Interlocked.Increment(ref callCount); signal.Release(); }, subscriberId: "c1");
        var sub2 = _hub.Subscribe("Test.Counter", (_, _) => { Interlocked.Increment(ref callCount); signal.Release(); }, subscriberId: "c2");
        var sub3 = _hub.Subscribe("Test.Counter", (_, _) => { Interlocked.Increment(ref callCount); signal.Release(); }, subscriberId: "c3");

        _hub.Publish("Test.Counter", """{"value":1}""", publisherId: "c4");
        await WaitFor(signal);
        await WaitFor(signal);
        await WaitFor(signal);

        callCount.ShouldBe(3);

        sub1.Dispose();
        sub2.Dispose();
        sub3.Dispose();
    }

    [Fact]
    public async Task Publish_OnlyNotifiesMatchingKey()
    {
        bool counterCalled = false;
        bool colorCalled = false;
        var signal = new SemaphoreSlim(0, 1);

        var sub1 = _hub.Subscribe("Test.Counter", (_, _) => { counterCalled = true; signal.Release(); });
        var sub2 = _hub.Subscribe("Test.Color", (_, _) => { colorCalled = true; });

        _hub.Publish("Test.Counter", """{"value":1}""", publisherId: "other");
        await WaitFor(signal);

        counterCalled.ShouldBeTrue();
        colorCalled.ShouldBeFalse();

        sub1.Dispose();
        sub2.Dispose();
    }

    // ---------------------------------------------------------------
    // Unsubscribe: disposing the subscription stops notifications
    // ---------------------------------------------------------------

    [Fact]
    public async Task Dispose_Subscription_StopsNotifications()
    {
        int callCount = 0;
        var signal = new SemaphoreSlim(0, 1);
        var sub = _hub.Subscribe("Test.Counter", (_, _) => { Interlocked.Increment(ref callCount); signal.Release(); });

        _hub.Publish("Test.Counter", """{"value":1}""", publisherId: "other");
        await WaitFor(signal);
        callCount.ShouldBe(1);

        sub.Dispose();

        _hub.Publish("Test.Counter", """{"value":2}""", publisherId: "other");
        await Task.Delay(100);
        callCount.ShouldBe(1); // no new call
    }

    // ---------------------------------------------------------------
    // Multiple keys per subscriber
    // ---------------------------------------------------------------

    [Fact]
    public async Task Subscribe_MultipleKeys_BothReceive()
    {
        string? lastKey = null;
        var signal = new SemaphoreSlim(0, 1);

        var sub1 = _hub.Subscribe("Test.Counter", (key, _) => { lastKey = key; signal.Release(); }, subscriberId: "c1");
        var sub2 = _hub.Subscribe("Test.Color", (key, _) => { lastKey = key; signal.Release(); }, subscriberId: "c1");

        _hub.Publish("Test.Counter", """{"value":1}""", publisherId: "c2");
        await WaitFor(signal);
        lastKey.ShouldBe("Test.Counter");

        _hub.Publish("Test.Color", """{"value":"red"}""", publisherId: "c2");
        await WaitFor(signal);
        lastKey.ShouldBe("Test.Color");

        sub1.Dispose();
        sub2.Dispose();
    }

    // ---------------------------------------------------------------
    // Thread safety: publish during subscribe/unsubscribe
    // ---------------------------------------------------------------

    [Fact]
    public async Task ConcurrentPublishAndSubscribe_DoesNotThrow()
    {
        var tasks = new List<Task>();

        for (int i = 0; i < 10; i++)
        {
            int idx = i;
            tasks.Add(Task.Run(async () =>
            {
                var signal = new SemaphoreSlim(0, 1);
                var sub = _hub.Subscribe($"Key.{idx}", (_, _) => signal.Release(), subscriberId: $"c{idx}");
                _hub.Publish($"Key.{idx}", """{"value":1}""", publisherId: "other");
                await WaitFor(signal);
                sub.Dispose();
            }));
        }

        await Should.NotThrowAsync(() => Task.WhenAll(tasks));
    }
}
