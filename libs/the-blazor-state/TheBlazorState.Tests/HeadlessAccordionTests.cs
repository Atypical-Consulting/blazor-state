using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Shouldly;
using TheBlazorState.Demo.Components.Headless.Accordion;
using Xunit;

namespace TheBlazorState.Tests;

public class HeadlessAccordionTests : IDisposable
{
    private readonly BunitContext _ctx = new();

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public void Accordion_AllItemsClosed_ByDefault()
    {
        var cut = _ctx.Render<TestAccordion>(p => p.Add(a => a.Multiple, false));
        cut.FindAll("[role='region']").Count.ShouldBe(0);
    }

    [Fact]
    public void Accordion_DefaultOpen_RendersItemOpen()
    {
        var cut = _ctx.Render<TestAccordionWithDefaultOpen>();
        cut.FindAll("[role='region']").Count.ShouldBe(1);
        cut.Find("[role='region']").TextContent.ShouldContain("Panel 1");
    }

    [Fact]
    public void Accordion_ClickTrigger_TogglesPanel()
    {
        var cut = _ctx.Render<TestAccordion>(p => p.Add(a => a.Multiple, false));

        cut.FindAll("[role='region']").Count.ShouldBe(0);

        // Click first trigger
        cut.FindAll("button[aria-expanded]")[0].Click();

        cut.FindAll("[role='region']").Count.ShouldBe(1);
    }

    [Fact]
    public void Accordion_SingleMode_ClosesOtherItems()
    {
        var cut = _ctx.Render<TestAccordion>(p => p.Add(a => a.Multiple, false));

        // Open first item
        cut.FindAll("button[aria-expanded]")[0].Click();
        cut.FindAll("[role='region']").Count.ShouldBe(1);

        // Open second item — first should close
        cut.FindAll("button[aria-expanded]")[1].Click();
        cut.FindAll("[role='region']").Count.ShouldBe(1);
    }

    [Fact]
    public void Accordion_MultipleMode_AllowsMultipleOpen()
    {
        var cut = _ctx.Render<TestAccordion>(p => p.Add(a => a.Multiple, true));

        cut.FindAll("button[aria-expanded]")[0].Click();
        cut.FindAll("button[aria-expanded]")[1].Click();

        cut.FindAll("[role='region']").Count.ShouldBe(2);
    }

    [Fact]
    public void Accordion_AriaExpanded_ReflectsState()
    {
        var cut = _ctx.Render<TestAccordion>(p => p.Add(a => a.Multiple, false));

        var trigger = cut.FindAll("button[aria-expanded]")[0];
        trigger.GetAttribute("aria-expanded").ShouldBe("false");

        trigger.Click();
        trigger = cut.FindAll("button[aria-expanded]")[0];
        trigger.GetAttribute("aria-expanded").ShouldBe("true");
    }

    [Fact]
    public void Accordion_AriaControls_LinksTriggerToPanel()
    {
        var cut = _ctx.Render<TestAccordion>(p => p.Add(a => a.Multiple, false));

        // Open first item
        cut.FindAll("button[aria-expanded]")[0].Click();

        var trigger = cut.FindAll("button[aria-expanded]")[0];
        var panelId = trigger.GetAttribute("aria-controls");
        panelId.ShouldNotBeNullOrEmpty();

        var panel = cut.Find($"[id='{panelId}']");
        panel.ShouldNotBeNull();
        panel.GetAttribute("role").ShouldBe("region");
    }

    /// <summary>Test accordion with 2 items, neither open by default</summary>
    private class TestAccordion : ComponentBase
    {
        [Parameter] public bool Multiple { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<Accordion>(0);
            b.AddAttribute(1, "Multiple", Multiple);
            b.AddAttribute(2, "ChildContent", (RenderFragment)(inner =>
            {
                BuildItem(inner, 10, "Trigger 1", "Panel 1");
                BuildItem(inner, 20, "Trigger 2", "Panel 2");
            }));
            b.CloseComponent();
        }

        private static void BuildItem(RenderTreeBuilder b, int seq, string triggerText, string panelText)
        {
            b.OpenComponent<AccordionItem>(seq);
            b.AddAttribute(seq + 1, "ChildContent", (RenderFragment)(item =>
            {
                item.OpenComponent<AccordionTrigger>(seq + 2);
                item.AddAttribute(seq + 3, "ChildContent", (RenderFragment)(t =>
                    t.AddContent(seq + 4, triggerText)));
                item.CloseComponent();

                item.OpenComponent<AccordionPanel>(seq + 5);
                item.AddAttribute(seq + 6, "ChildContent", (RenderFragment)(p =>
                    p.AddContent(seq + 7, panelText)));
                item.CloseComponent();
            }));
            b.CloseComponent();
        }
    }

    /// <summary>Test accordion with first item open by default</summary>
    private class TestAccordionWithDefaultOpen : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<Accordion>(0);
            b.AddAttribute(1, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenComponent<AccordionItem>(10);
                inner.AddAttribute(11, "DefaultOpen", true);
                inner.AddAttribute(12, "ChildContent", (RenderFragment)(item =>
                {
                    item.OpenComponent<AccordionTrigger>(13);
                    item.AddAttribute(14, "ChildContent", (RenderFragment)(t =>
                        t.AddContent(15, "Trigger 1")));
                    item.CloseComponent();

                    item.OpenComponent<AccordionPanel>(16);
                    item.AddAttribute(17, "ChildContent", (RenderFragment)(p =>
                        p.AddContent(18, "Panel 1")));
                    item.CloseComponent();
                }));
                inner.CloseComponent();
            }));
            b.CloseComponent();
        }
    }
}
