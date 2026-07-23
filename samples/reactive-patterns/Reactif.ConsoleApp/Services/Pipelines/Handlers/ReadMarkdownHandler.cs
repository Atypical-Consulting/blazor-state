using Reactif.Abstractions.Pipelines;
using Reactif.ConsoleApp.Services.Pipelines.Models;

namespace Reactif.ConsoleApp.Services.Pipelines.Handlers;

public class ReadMarkdownHandler
    : Handler<FilePath, FileContentResult>
{
    private readonly IFileService _fileService;
    private readonly ILogger _logger;
    
    public ReadMarkdownHandler(
        IFileService fileService,
        ILogger logger)
    {
        _fileService = fileService;
        _logger = logger;
    }
    
    protected override FileContentResult Handle(FilePath filePath)
    {
        _logger.LogInformation("Reading markdown file: {InputValue}", filePath.Value);
        
        // var markdown = _fileService.ReadAllTextAsync(filePath.Value)
        //     .ConfigureAwait(false)
        //     .GetAwaiter()
        //     .GetResult();
        
        return new FileContentResult("# markdown", filePath);
    }
}