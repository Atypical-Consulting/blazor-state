using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Shouldly;
using TheBlazorState.Demo.Components.Headless.Tabs;
using Xunit;

namespace TheBlazorState.Tests;

public class HeadlessTabsTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public void Tabs_FirstTabSelected_ByDefault()
    {
        var cut = _ctx.Render<TestTabs>();

        var tabs = cut.FindAll("[role='tab']");
        tabs[0].GetAttribute("aria-selected").ShouldBe("true");
        tabs[1].GetAttribute("aria-selected").ShouldBe("false");
    }

    [Fact]
    public void Tabs_ClickTab_ChangesSelectedPanel()
    {
        var cut = _ctx.Render<TestTabs>();

        // First panel visible
        cut.FindAll("[role='tabpanel']").Count.ShouldBe(1);
        cut.Find("[role='tabpanel']").TextContent.ShouldContain("Content 1");

        // Click second tab
        cut.FindAll("[role='tab']")[1].Click();

        cut.Find("[role='tabpanel']").TextContent.ShouldContain("Content 2");
    }

    [Fact]
    public void Tabs_RolesPresent()
    {
        var cut = _ctx.Render<TestTabs>();

        cut.Find("[role='tablist']").ShouldNotBeNull();
        cut.FindAll("[role='tab']").Count.ShouldBe(2);
        cut.FindAll("[role='tabpanel']").Count.ShouldBe(1);
    }

    [Fact]
    public void Tabs_AriaSelected_UpdatesOnClick()
    {
        var cut = _ctx.Render<TestTabs>();

        cut.FindAll("[role='tab']")[1].Click();

        cut.FindAll("[role='tab']")[0].GetAttribute("aria-selected").ShouldBe("false");
        cut.FindAll("[role='tab']")[1].GetAttribute("aria-selected").ShouldBe("true");
    }

    [Fact]
    public void Tabs_InactivePanels_NotInDom()
    {
        var cut = _ctx.Render<TestTabs>();

        // Only 1 panel should be rendered
        cut.FindAll("[role='tabpanel']").Count.ShouldBe(1);
    }

    [Fact]
    public void Tabs_BindSelectedIndex_Works()
    {
        var selectedIndex = 0;
        var cut = _ctx.Render<TestTabsWithBind>(p => p
            .Add(t => t.SelectedIndex, selectedIndex)
            .Add(t => t.SelectedIndexChanged, v => selectedIndex = v));

        cut.FindAll("[role='tab']")[1].Click();
        selectedIndex.ShouldBe(1);
    }

    /// <summary>Simple 2-tab test component</summary>
    private class TestTabs : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<TabGroup>(0);
            b.AddAttribute(1, "ChildContent", (RenderFragment)(inner =>
            {
                // TabList with Tabs
                inner.OpenComponent<TabList>(10);
                inner.AddAttribute(11, "ChildContent", (RenderFragment)(list =>
                {
                    list.OpenComponent<Tab>(12);
                    list.AddAttribute(13, "ChildContent", (RenderFragment)(t =>
                        t.AddContent(14, "Tab 1")));
                    list.CloseComponent();

                    list.OpenComponent<Tab>(15);
                    list.AddAttribute(16, "ChildContent", (RenderFragment)(t =>
                        t.AddContent(17, "Tab 2")));
                    list.CloseComponent();
                }));
                inner.CloseComponent();

                // TabPanels
                inner.OpenComponent<TabPanels>(20);
                inner.AddAttribute(21, "ChildContent", (RenderFragment)(panels =>
                {
                    panels.OpenComponent<TabPanel>(22);
                    panels.AddAttribute(23, "ChildContent", (RenderFragment)(p =>
                        p.AddContent(24, "Content 1")));
                    panels.CloseComponent();

                    panels.OpenComponent<TabPanel>(25);
                    panels.AddAttribute(26, "ChildContent", (RenderFragment)(p =>
                        p.AddContent(27, "Content 2")));
                    panels.CloseComponent();
                }));
                inner.CloseComponent();
            }));
            b.CloseComponent();
        }
    }

    /// <summary>Tabs with bindable SelectedIndex</summary>
    private class TestTabsWithBind : ComponentBase
    {
        [Parameter] public int SelectedIndex { get; set; }
        [Parameter] public EventCallback<int> SelectedIndexChanged { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<TabGroup>(0);
            b.AddAttribute(1, "SelectedIndex", SelectedIndex);
            b.AddAttribute(2, "SelectedIndexChanged", SelectedIndexChanged);
            b.AddAttribute(3, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<TabList>(10);
                inner.AddAttribute(11, "ChildContent", (RenderFragment)(list =>
                {
                    list.OpenComponent<Tab>(12);
                    list.AddAttribute(13, "ChildContent", (RenderFragment)(t =>
                        t.AddContent(14, "Tab 1")));
                    list.CloseComponent();

                    list.OpenComponent<Tab>(15);
                    list.AddAttribute(16, "ChildContent", (RenderFragment)(t =>
                        t.AddContent(17, "Tab 2")));
                    list.CloseComponent();
                }));
                inner.CloseComponent();

                inner.OpenComponent<TabPanels>(20);
                inner.AddAttribute(21, "ChildContent", (RenderFragment)(panels =>
                {
                    panels.OpenComponent<TabPanel>(22);
                    panels.AddAttribute(23, "ChildContent", (RenderFragment)(p =>
                        p.AddContent(24, "Content 1")));
                    panels.CloseComponent();

                    panels.OpenComponent<TabPanel>(25);
                    panels.AddAttribute(26, "ChildContent", (RenderFragment)(p =>
                        p.AddContent(27, "Content 2")));
                    panels.CloseComponent();
                }));
                inner.CloseComponent();
            }));
            b.CloseComponent();
        }
    }
}
