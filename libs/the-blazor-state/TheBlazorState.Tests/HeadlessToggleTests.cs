using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Shouldly;
using TheBlazorState.Demo.Components.Headless.Toggle;
using Xunit;

namespace TheBlazorState.Tests;

public class HeadlessToggleTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public void Toggle_RendersWithSwitchRole()
    {
        var cut = _ctx.Render<Toggle>(p => p.Add(t => t.Value, false));
        var element = cut.Find("[role='switch']");
        element.ShouldNotBeNull();
    }

    [Fact]
    public void Toggle_AriaCheckedFalse_WhenValueIsFalse()
    {
        var cut = _ctx.Render<Toggle>(p => p.Add(t => t.Value, false));
        cut.Find("[role='switch']").GetAttribute("aria-checked").ShouldBe("false");
    }

    [Fact]
    public void Toggle_AriaCheckedTrue_WhenValueIsTrue()
    {
        var cut = _ctx.Render<Toggle>(p => p.Add(t => t.Value, true));
        cut.Find("[role='switch']").GetAttribute("aria-checked").ShouldBe("true");
    }

    [Fact]
    public void Toggle_Click_TogglesValue()
    {
        var value = false;
        var cut = _ctx.Render<Toggle>(p => p
            .Add(t => t.Value, value)
            .Add(t => t.ValueChanged, v => value = v));

        cut.Find("[role='switch']").Click();
        value.ShouldBeTrue();
    }

    [Fact]
    public void Toggle_Click_UpdatesAriaChecked()
    {
        var value = false;
        var cut = _ctx.Render<Toggle>(p => p
            .Add(t => t.Value, value)
            .Add(t => t.ValueChanged, v => value = v));

        cut.Find("[role='switch']").Click();

        // Value should have toggled via callback
        value.ShouldBeTrue();

        // Re-render with new value to verify aria-checked update
        cut.Render(p => p
            .Add(t => t.Value, true)
            .Add(t => t.ValueChanged, EventCallback.Factory.Create<bool>(this, v => value = v)));
        cut.Find("[role='switch']").GetAttribute("aria-checked").ShouldBe("true");
    }

    [Fact]
    public void Toggle_RendersAsButton_ByDefault()
    {
        var cut = _ctx.Render<Toggle>(p => p.Add(t => t.Value, false));
        cut.Find("button").ShouldNotBeNull();
    }

    [Fact]
    public void Toggle_AsParameter_ChangesElement()
    {
        var cut = _ctx.Render<Toggle>(p => p
            .Add(t => t.Value, false)
            .Add(t => t.As, "div"));
        cut.Find("div[role='switch']").ShouldNotBeNull();
    }

    [Fact]
    public void Toggle_ClassParameter_Applied()
    {
        var cut = _ctx.Render<Toggle>(p => p
            .Add(t => t.Value, false)
            .Add(t => t.Class, "my-toggle"));
        cut.Find("[role='switch']").ClassList.ShouldContain("my-toggle");
    }

    [Fact]
    public void Toggle_Label_SetsAriaLabel()
    {
        var cut = _ctx.Render<Toggle>(p => p
            .Add(t => t.Value, false)
            .Add(t => t.Label, "Dark mode"));
        cut.Find("[role='switch']").GetAttribute("aria-label").ShouldBe("Dark mode");
    }

    [Fact]
    public void Toggle_AdditionalAttributes_Forwarded()
    {
        var cut = _ctx.Render<Toggle>(p => p
            .Add(t => t.Value, false)
            .AddUnmatched("data-testid", "my-toggle"));
        cut.Find("[data-testid='my-toggle']").ShouldNotBeNull();
    }
}
