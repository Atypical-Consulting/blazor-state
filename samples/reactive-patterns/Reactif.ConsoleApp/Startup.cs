using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reactif.ConsoleApp.Config;
using Reactif.ConsoleApp.Services;
using Reactif.ConsoleApp.Services.FileWatchers;
using Reactif.ConsoleApp.Services.Pipelines;

namespace Reactif.ConsoleApp;

/// <summary>
/// This class is responsible for configuring the application's services.
/// </summary>
public static class Startup
{
    /// <summary>
    /// Configures the application's services.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    public static void RegisterServices(this IServiceCollection services)
    {
        var configuration = BuildConfiguration();
        services.Configure<AppConfiguration>(configuration);
        
        services.AddSingleton<IMarkdownPipelineFactory, MarkdownPipelineFactory>();
        services.AddSingleton<IHtmlStyler, HtmlStyler>();
        services.AddSingleton<IHtmlComposer, HtmlComposer>();
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IFileWatcher, MarkdownFileWatcher>();
        services.AddSingleton<IMarkdownToHtmlFileProcessor, MarkdownToHtmlFileProcessor>();
        services.AddSingleton<IMarkdownToHtmlConverter, MarkdownToHtmlConverter>();
        services.AddSingleton<FileProcessingOrchestrator>();
        services.AddSingleton<ChainOfResponsibilityExample>();
    }

    private static IConfigurationRoot BuildConfiguration()
        => new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", false, true)
            .Build();
}
