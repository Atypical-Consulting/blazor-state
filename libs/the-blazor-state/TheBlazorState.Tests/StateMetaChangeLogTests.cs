using Shouldly;
using TheBlazorState.Abstractions;
using Xunit;

namespace TheBlazorState.Tests;

/// <summary>
/// Tests for StateMeta's built-in change log.
/// The change log records value transitions and is capped to a max size.
/// It's written by the library (not by demo code), eliminating duplicates.
/// </summary>
public class StateMetaChangeLogTests
{
    [Fact]
    public void ChangeLog_EmptyByDefault()
    {
        var meta = new StateMeta(ttl: null);
        meta.ChangeLog.ShouldBeEmpty();
    }

    [Fact]
    public void LogChange_AddsEntry()
    {
        var meta = new StateMeta(ttl: null);

        meta.LogChange("0", "42");

        meta.ChangeLog.Count.ShouldBe(1);
        meta.ChangeLog[0].OldValue.ShouldBe("0");
        meta.ChangeLog[0].NewValue.ShouldBe("42");
    }

    [Fact]
    public void LogChange_NewestFirst()
    {
        var meta = new StateMeta(ttl: null);

        meta.LogChange("0", "1");
        meta.LogChange("1", "2");
        meta.LogChange("2", "3");

        meta.ChangeLog[0].NewValue.ShouldBe("3");
        meta.ChangeLog[1].NewValue.ShouldBe("2");
        meta.ChangeLog[2].NewValue.ShouldBe("1");
    }

    [Fact]
    public void LogChange_HasTimestamp()
    {
        var meta = new StateMeta(ttl: null);
        var before = DateTimeOffset.UtcNow;

        meta.LogChange("a", "b");

        var after = DateTimeOffset.UtcNow;
        meta.ChangeLog[0].Timestamp.ShouldBeInRange(before, after);
    }

    [Fact]
    public void LogChange_CapsAtMaxSize()
    {
        var meta = new StateMeta(ttl: null);

        for (int i = 0; i < 20; i++)
            meta.LogChange(i.ToString(), (i + 1).ToString());

        meta.ChangeLog.Count.ShouldBeLessThanOrEqualTo(10);
        // Most recent entry should be the last one added
        meta.ChangeLog[0].NewValue.ShouldBe("20");
    }

    [Fact]
    public void LogChange_RecordsSource()
    {
        var meta = new StateMeta(ttl: null);

        meta.LogChange("0", "1", source: ChangeSource.Local);
        meta.LogChange("1", "2", source: ChangeSource.CrossTab);

        meta.ChangeLog[0].Source.ShouldBe(ChangeSource.CrossTab);
        meta.ChangeLog[1].Source.ShouldBe(ChangeSource.Local);
    }

    [Fact]
    public void LogChange_DefaultSourceIsLocal()
    {
        var meta = new StateMeta(ttl: null);

        meta.LogChange("0", "1");

        meta.ChangeLog[0].Source.ShouldBe(ChangeSource.Local);
    }
}
