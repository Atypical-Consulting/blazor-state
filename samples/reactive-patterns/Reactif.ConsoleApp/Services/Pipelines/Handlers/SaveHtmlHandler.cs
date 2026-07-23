using Reactif.Abstractions.Pipelines;

namespace Reactif.ConsoleApp.Services.Pipelines.Handlers;

public class SaveHtmlHandler
    : Handler<string, string>
{
    private readonly string _outputPath;
    private readonly ILogger _logger;

    public SaveHtmlHandler(string outputPath, ILogger logger)
    {
        _outputPath = outputPath;
        _logger = logger;
    }

    protected override string Handle(string input)
    {
        _logger.LogInformation("Saving HTML to: {OutputPath}", _outputPath);
        return _outputPath;
    }
}