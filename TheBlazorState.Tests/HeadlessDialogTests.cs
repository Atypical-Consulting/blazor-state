using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using TheBlazorState.Demo.Components.Headless.Dialog;
using TheBlazorState.Demo.Services;
using Xunit;

namespace TheBlazorState.Tests;

public class HeadlessDialogTests : IDisposable
{
    private readonly BunitContext _ctx;

    public HeadlessDialogTests()
    {
        _ctx = new BunitContext();
        var module = _ctx.JSInterop.SetupModule("./js/headless.module.js");
        module.SetupVoid("trapFocus", _ => true);
        module.SetupVoid("releaseFocus", _ => true);
        module.SetupVoid("focusElement", _ => true);
        module.SetupVoid("onClickOutside", _ => true);
        module.SetupVoid("removeClickOutside", _ => true);
        _ctx.Services.AddScoped<HeadlessJsModule>();
    }

    public void Dispose() => _ctx.Dispose();

    [Fact]
    public void Dialog_NotRendered_WhenClosed()
    {
        var cut = _ctx.Render<TestDialog>(p => p.Add(d => d.Open, false));
        cut.FindAll("[role='dialog']").Count.ShouldBe(0);
    }

    [Fact]
    public void Dialog_Rendered_WhenOpen()
    {
        var cut = _ctx.Render<TestDialog>(p => p.Add(d => d.Open, true));
        cut.Find("[role='dialog']").ShouldNotBeNull();
    }

    [Fact]
    public void Dialog_RoleAndAriaModal_Present()
    {
        var cut = _ctx.Render<TestDialog>(p => p.Add(d => d.Open, true));
        var dialog = cut.Find("[role='dialog']");
        dialog.GetAttribute("aria-modal").ShouldBe("true");
    }

    [Fact]
    public void Dialog_AriaLabelledby_LinksToTitle()
    {
        var cut = _ctx.Render<TestDialog>(p => p.Add(d => d.Open, true));
        var dialog = cut.Find("[role='dialog']");
        var labelledBy = dialog.GetAttribute("aria-labelledby");
        labelledBy.ShouldNotBeNullOrEmpty();

        var title = cut.Find($"[id='{labelledBy}']");
        title.ShouldNotBeNull();
        title.TextContent.ShouldContain("Test Title");
    }

    [Fact]
    public void DialogClose_Click_ClosesDialog()
    {
        var open = true;
        var cut = _ctx.Render<TestDialog>(p => p
            .Add(d => d.Open, open)
            .Add(d => d.OpenChanged, v => open = v));

        cut.Find("[role='dialog']").ShouldNotBeNull();

        // Click the close button
        cut.Find("[data-testid='close-btn']").Click();
        open.ShouldBeFalse();
    }

    [Fact]
    public void DialogOverlay_Click_ClosesDialog()
    {
        var open = true;
        var cut = _ctx.Render<TestDialog>(p => p
            .Add(d => d.Open, open)
            .Add(d => d.OpenChanged, v => open = v));

        cut.Find("[data-testid='overlay']").Click();
        open.ShouldBeFalse();
    }

    /// <summary>Test dialog component wrapping Dialog family</summary>
    private class TestDialog : ComponentBase
    {
        [Parameter] public bool Open { get; set; }
        [Parameter] public EventCallback<bool> OpenChanged { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder b)
        {
            b.OpenComponent<Dialog>(0);
            b.AddAttribute(1, "Open", Open);
            b.AddAttribute(2, "OpenChanged", OpenChanged);
            b.AddAttribute(3, "ChildContent", (RenderFragment)(inner =>
            {
                // Trigger
                inner.OpenComponent<DialogTrigger>(10);
                inner.AddAttribute(11, "ChildContent", (RenderFragment)(t =>
                    t.AddContent(12, "Open")));
                inner.CloseComponent();

                // Overlay
                inner.OpenComponent<DialogOverlay>(20);
                inner.AddMultipleAttributes(21, new Dictionary<string, object>
                {
                    ["data-testid"] = "overlay"
                });
                inner.CloseComponent();

                // Content
                inner.OpenComponent<DialogContent>(30);
                inner.AddAttribute(31, "ChildContent", (RenderFragment)(content =>
                {
                    // Title
                    content.OpenComponent<DialogTitle>(32);
                    content.AddAttribute(33, "ChildContent", (RenderFragment)(t =>
                        t.AddContent(34, "Test Title")));
                    content.CloseComponent();

                    // Close button
                    content.OpenComponent<DialogClose>(35);
                    content.AddMultipleAttributes(36, new Dictionary<string, object>
                    {
                        ["data-testid"] = "close-btn"
                    });
                    content.AddAttribute(37, "ChildContent", (RenderFragment)(c =>
                        c.AddContent(38, "Close")));
                    content.CloseComponent();
                }));
                inner.CloseComponent();
            }));
            b.CloseComponent();
        }
    }
}
