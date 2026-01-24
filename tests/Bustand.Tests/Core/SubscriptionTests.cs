using Bustand.Core;
using Bustand.Tests.TestStores;

namespace Bustand.Tests.Core;

/// <summary>
/// Tests for the subscription system covering selector-based change detection and disposal.
/// </summary>
public class SubscriptionTests
{
    // Subscribe to all state changes
    [Fact]
    public void Subscribe_AllChanges_NotifiesOnEverySet()
    {
        var store = new CounterStore();
        var notifications = 0;
        store.Subscribe(() => notifications++);

        store.Increment();
        store.Increment();
        store.Decrement();

        Assert.Equal(3, notifications);
    }

    // Subscribe to slice with selector (reference type - string)
    // Note: Reference equality is used per CONTEXT.md. For value types (int),
    // every state change creates a new boxed reference, so they always notify.
    // Use reference types (string, record) for true slice-based filtering.
    [Fact]
    public void Subscribe_WithSelector_NotifiesOnlyWhenSliceChanges()
    {
        var store = new MultiPropertyStore();
        var nameNotifications = 0;

        // Use string selector (reference type) to test slice-based filtering
        store.Subscribe(s => s.Name, () => nameNotifications++);

        store.SetName("Alice"); // Name changes -> notify
        store.SetCount(1);       // Count changes, Name same reference ("Alice") -> no notify
        store.SetName("Bob");    // Name changes -> notify
        store.SetCount(2);       // Count changes, Name same reference ("Bob") -> no notify

        Assert.Equal(2, nameNotifications);
    }

    // Selector uses reference equality
    [Fact]
    public void Subscribe_WithSelector_UsesReferenceEquality()
    {
        var store = new CounterStore();
        var notifications = 0;
        store.Subscribe(s => s.Count, () => notifications++);

        store.SetCount(5);  // Change
        store.SetCount(5);  // Same value but new record -> notify (reference differs)

        // Records with same values are structurally equal but reference-different
        // Per CONTEXT.md: use reference equality
        Assert.Equal(2, notifications);
    }

    // Dispose stops notifications
    [Fact]
    public void Subscription_Dispose_StopsNotifications()
    {
        var store = new CounterStore();
        var notifications = 0;
        var sub = store.Subscribe(() => notifications++);

        store.Increment();
        Assert.Equal(1, notifications);

        sub.Dispose();

        store.Increment();
        Assert.Equal(1, notifications); // No new notification
    }

    // IsActive property
    [Fact]
    public void Subscription_IsActive_TrueUntilDisposed()
    {
        var store = new CounterStore();
        var sub = store.Subscribe(() => { });

        Assert.True(sub.IsActive);
        sub.Dispose();
        Assert.False(sub.IsActive);
    }

    // Multiple subscriptions
    [Fact]
    public void Store_MultipleSubscriptions_AllReceiveNotifications()
    {
        var store = new CounterStore();
        var count1 = 0;
        var count2 = 0;
        store.Subscribe(() => count1++);
        store.Subscribe(() => count2++);

        store.Increment();

        Assert.Equal(1, count1);
        Assert.Equal(1, count2);
    }

    // SubscriptionCount tracking
    [Fact]
    public void SubscriptionCount_TracksActiveSubscriptions()
    {
        var store = new CounterStore();
        Assert.Equal(0, store.SubscriptionCount);

        var sub1 = store.Subscribe(() => { });
        Assert.Equal(1, store.SubscriptionCount);

        var sub2 = store.Subscribe(s => s.Count, () => { });
        Assert.Equal(2, store.SubscriptionCount);

        sub1.Dispose();
        Assert.Equal(1, store.SubscriptionCount);

        sub2.Dispose();
        Assert.Equal(0, store.SubscriptionCount);
    }

    // Disposal during notification (graceful)
    [Fact]
    public void Subscribe_DisposeDuringNotification_HandledGracefully()
    {
        var store = new CounterStore();
        ISubscription? sub = null;
        sub = store.Subscribe(() =>
        {
            sub?.Dispose(); // Dispose during callback
        });

        // Should not throw
        store.Increment();
        Assert.False(sub!.IsActive);
    }

    // COMP-08: Subscriptions dispose properly
    [Fact]
    public void Subscriptions_DisposeProperly_NoMemoryLeak()
    {
        var store = new CounterStore();
        var subs = new List<ISubscription>();

        for (int i = 0; i < 100; i++)
        {
            subs.Add(store.Subscribe(() => { }));
        }
        Assert.Equal(100, store.SubscriptionCount);

        foreach (var sub in subs)
        {
            sub.Dispose();
        }
        Assert.Equal(0, store.SubscriptionCount);
    }

    // Test Unsubscribe method
    [Fact]
    public void Subscription_Unsubscribe_StopsNotifications()
    {
        var store = new CounterStore();
        var notifications = 0;
        var sub = store.Subscribe(() => notifications++);

        store.Increment();
        Assert.Equal(1, notifications);

        sub.Unsubscribe();

        store.Increment();
        Assert.Equal(1, notifications);
        Assert.False(sub.IsActive);
    }

    // Test selector subscription with name property
    [Fact]
    public void Subscribe_NameSelector_OnlyNotifiesOnNameChange()
    {
        var store = new MultiPropertyStore();
        var nameNotifications = 0;
        store.Subscribe(s => s.Name, () => nameNotifications++);

        store.SetName("Alice"); // Name changes -> notify
        store.SetCount(42);     // Count changes, Name same -> no notify
        store.SetName("Bob");   // Name changes -> notify

        Assert.Equal(2, nameNotifications);
    }

    // Test full state subscription with MultiPropertyStore
    [Fact]
    public void Subscribe_FullState_NotifiesOnAllChanges()
    {
        var store = new MultiPropertyStore();
        var notifications = 0;
        store.Subscribe(() => notifications++);

        store.SetName("Alice");
        store.SetCount(1);
        store.SetName("Bob");
        store.SetCount(2);

        Assert.Equal(4, notifications);
    }
}
