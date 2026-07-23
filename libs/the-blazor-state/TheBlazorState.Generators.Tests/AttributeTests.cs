using TheBlazorState.Attributes;
using Shouldly;
using Xunit;

namespace TheBlazorState.Generators.Tests;

public class AttributeTests
{
    [Fact]
    public void PersistAttribute_Can_Be_Constructed()
    {
        var attr = new PersistAttribute();
        attr.TimeToLive.ShouldBeNull();
    }

    [Fact]
    public void PersistAttribute_Accepts_TimeToLive()
    {
        var attr = new PersistAttribute { TimeToLive = "00:05:00" };
        attr.TimeToLive.ShouldBe("00:05:00");
    }

    [Fact]
    public void SharedAttribute_Can_Be_Constructed()
    {
        var attr = new SharedAttribute();
        attr.ShouldNotBeNull();
    }
}
