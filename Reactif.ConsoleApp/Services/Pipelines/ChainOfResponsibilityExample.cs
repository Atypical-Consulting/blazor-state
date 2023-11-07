using Reactif.ConsoleApp.Services.Pipelines.Handlers;
using Reactif.ConsoleApp.Services.Pipelines.Models;

namespace Reactif.ConsoleApp.Services.Pipelines;

public class ChainOfResponsibilityExample
{
    private readonly ILogger<ChainOfResponsibilityExample> _logger;

    public ChainOfResponsibilityExample(
        ILogger<ChainOfResponsibilityExample> logger)
    {
        _logger = logger;
    }

    public void Execute()
    {
        // Set up the chain of responsibility
        var readMarkdown = new ReadMarkdownHandler(_logger);
        var convertToHtml = new ConvertMarkdownToHtmlHandler(_logger);
        var saveHtml = new SaveHtmlHandler("output.html", _logger);

        // Link the handlers
        readMarkdown.Subscribe(convertToHtml);
        convertToHtml.Subscribe(saveHtml);

        // The final handler does something with the result (like outputting the file path)
        saveHtml.Subscribe(resultPath => _logger.LogInformation($"File saved at: {resultPath}"));

        // Start the chain by reading a markdown file
        var input = new FilePath("path/to/markdown.md");
        readMarkdown.OnNext(input);
    }
}