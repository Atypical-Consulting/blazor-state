using Reactif.Abstractions.Pipelines;
using Reactif.ConsoleApp.Services.Pipelines.Models;

namespace Reactif.ConsoleApp.Services.Pipelines.Handlers;

public class ConvertMarkdownToHtmlHandler
    : Handler<FileContentResult, string>
{
    private readonly ILogger _logger;

    public ConvertMarkdownToHtmlHandler(ILogger logger)
    {
        _logger = logger;
    }

    protected override string Handle(FileContentResult input)
    {
        _logger.LogInformation("Converting markdown to HTML");
        return "<html><body>" + input.Content + "</body></html>";
    }
}