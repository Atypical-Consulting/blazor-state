using Microsoft.Extensions.Options;
using Reactif.ConsoleApp.Config;

namespace Reactif.ConsoleApp.Services.FileWatchers;

public class MarkdownFileWatcher : ReactiveFileWatcher
{
    protected override string FileFilter => "*.md";

    public MarkdownFileWatcher(
        IOptions<AppConfiguration> options,
        ILogger<MarkdownFileWatcher> logger)
        : base(options, logger)
    {
    }
}