using Reactif.Abstractions.Pipelines;
using Reactif.ConsoleApp.Services.Pipelines.Models;

namespace Reactif.ConsoleApp.Services.Pipelines.Handlers;

public class ReadMarkdownHandler
    : Handler<FilePath, FileContentResult>
{
    private readonly ILogger _logger;
    
    public ReadMarkdownHandler(ILogger logger)
    {
        _logger = logger;
    }
    
    protected override FileContentResult Handle(FilePath input)
    {
        _logger.LogInformation("Reading markdown file: {InputValue}", input.Value);
        return new FileContentResult("Markdown content", input.Value);
    }
}