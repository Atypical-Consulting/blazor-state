using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Shouldly;
using TheBlazorState.Demo.Components.Headless.Dropdown;
using TheBlazorState.Demo.Services;
using Xunit;

namespace TheBlazorState.Tests;

public class HeadlessDropdownTests : IDisposable
{
    private readonly BunitContext _ctx;

    public HeadlessDropdownTests()
    {
        _ctx = new BunitContext();
        var module = _ctx.JSInterop.SetupModule("./js/headless.module.js");
        module.SetupVoid("onClickOutside", _ => true);
        module.SetupVoid("removeClickOutside", _ => true);
        module.SetupVoid("focusElement", _ => true);
        _ctx.Services.AddScoped<HeadlessJsModule>();
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public void Dropdown_PanelHidden_ByDefault()
    {
        var cut = _ctx.Render<TestDropdown>();
        cut.FindAll("[role='menu']").Count.ShouldBe(0);
    }

    [Fact]
    public void Dropdown_ClickTrigger_OpensPanel()
    {
        var cut = _ctx.Render<TestDropdown>();

        cut.Find("[aria-haspopup]").Click();

        cut.FindAll("[role='menu']").Count.ShouldBe(1);
    }

    [Fact]
    public void Dropdown_MenuRoles_Present()
    {
        var cut = _ctx.Render<TestDropdown>();
        cut.Find("[aria-haspopup]").Click();

        cut.Find("[role='menu']").ShouldNotBeNull();
        cut.FindAll("[role='menuitem']").Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Dropdown_AriaExpanded_ReflectsState()
    {
        var cut = _ctx.Render<TestDropdown>();

        cut.Find("[aria-haspopup]").GetAttribute("aria-expanded").ShouldBe("false");

        cut.Find("[aria-haspopup]").Click();
        cut.Find("[aria-haspopup]").GetAttribute("aria-expanded").ShouldBe("true");
    }

    [Fact]
    public void Dropdown_Separator_HasRole()
    {
        var cut = _ctx.Render<TestDropdownWithSeparator>();
        cut.Find("[aria-haspopup]").Click();

        cut.Find("[role='separator']").ShouldNotBeNull();
    }

    [Fact]
    public void Dropdown_DisabledItem_HasAriaDisabled()
    {
        var cut = _ctx.Render<TestDropdownWithDisabledItem>();
        cut.Find("[aria-haspopup]").Click();

        cut.FindAll("[role='menuitem']")[1].GetAttribute("aria-disabled").ShouldBe("true");
    }

    /// <summary>Simple dropdown with 2 items</summary>
    private class TestDropdown : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<Dropdown>(0);
            b.AddAttribute(1, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<DropdownTrigger>(10);
                inner.AddAttribute(11, "ChildContent", (RenderFragment)(t =>
                    t.AddContent(12, "Actions")));
                inner.CloseComponent();

                inner.OpenComponent<DropdownPanel>(20);
                inner.AddAttribute(21, "ChildContent", (RenderFragment)(panel =>
                {
                    panel.OpenComponent<DropdownItem>(22);
                    panel.AddAttribute(23, "ChildContent", (RenderFragment)(i =>
                        i.AddContent(24, "Edit")));
                    panel.CloseComponent();

                    panel.OpenComponent<DropdownItem>(25);
                    panel.AddAttribute(26, "ChildContent", (RenderFragment)(i =>
                        i.AddContent(27, "Delete")));
                    panel.CloseComponent();
                }));
                inner.CloseComponent();
            }));
            b.CloseComponent();
        }
    }

    /// <summary>Dropdown with a separator</summary>
    private class TestDropdownWithSeparator : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<Dropdown>(0);
            b.AddAttribute(1, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<DropdownTrigger>(10);
                inner.AddAttribute(11, "ChildContent", (RenderFragment)(t =>
                    t.AddContent(12, "Actions")));
                inner.CloseComponent();

                inner.OpenComponent<DropdownPanel>(20);
                inner.AddAttribute(21, "ChildContent", (RenderFragment)(panel =>
                {
                    panel.OpenComponent<DropdownItem>(22);
                    panel.AddAttribute(23, "ChildContent", (RenderFragment)(i =>
                        i.AddContent(24, "Edit")));
                    panel.CloseComponent();

                    panel.OpenComponent<DropdownSeparator>(25);
                    panel.CloseComponent();

                    panel.OpenComponent<DropdownItem>(28);
                    panel.AddAttribute(29, "ChildContent", (RenderFragment)(i =>
                        i.AddContent(30, "Delete")));
                    panel.CloseComponent();
                }));
                inner.CloseComponent();
            }));
            b.CloseComponent();
        }
    }

    /// <summary>Dropdown with a disabled item</summary>
    private class TestDropdownWithDisabledItem : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<Dropdown>(0);
            b.AddAttribute(1, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<DropdownTrigger>(10);
                inner.AddAttribute(11, "ChildContent", (RenderFragment)(t =>
                    t.AddContent(12, "Actions")));
                inner.CloseComponent();

                inner.OpenComponent<DropdownPanel>(20);
                inner.AddAttribute(21, "ChildContent", (RenderFragment)(panel =>
                {
                    panel.OpenComponent<DropdownItem>(22);
                    panel.AddAttribute(23, "ChildContent", (RenderFragment)(i =>
                        i.AddContent(24, "Edit")));
                    panel.CloseComponent();

                    panel.OpenComponent<DropdownItem>(25);
                    panel.AddAttribute(26, "Disabled", true);
                    panel.AddAttribute(27, "ChildContent", (RenderFragment)(i =>
                        i.AddContent(28, "Delete")));
                    panel.CloseComponent();
                }));
                inner.CloseComponent();
            }));
            b.CloseComponent();
        }
    }
}
