using Microsoft.Extensions.Options;
using Reactif.ConsoleApp.Config;

namespace Reactif.ConsoleApp.Services.FileWatchers;

public class TextFileWatcher : ReactiveFileWatcher
{
    protected override string FileFilter => "*.txt";

    public TextFileWatcher(
        IOptions<AppConfiguration> options,
        ILogger<TextFileWatcher> logger)
        : base(options, logger)
    {
    }
}