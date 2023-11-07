using Microsoft.Extensions.Hosting;
using Reactif.ConsoleApp.Services;

namespace Reactif.ConsoleApp.Run;

/// <summary>
/// Represents the application.
/// </summary>
public class App : IHostedService
{
    private readonly FileProcessingOrchestrator _orchestrator;
    private readonly ILogger<App> _logger;
    private IDisposable? _processFilesSubscription;

    public App(
        FileProcessingOrchestrator orchestrator,
        ILogger<App> logger)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.Starting();
        _processFilesSubscription = _orchestrator.ProcessFiles();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.Stopping();
        _processFilesSubscription?.Dispose();
        return Task.CompletedTask;
    }
}

internal static partial class AppLoggerExtensions
{
    [LoggerMessage(
        EventId = 0,
        Level = LogLevel.Information,
        Message = "Starting Reactif")]
    public static partial void Starting(
        this ILogger<App> logger);
    
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Stopping Reactif")]
    public static partial void Stopping(
        this ILogger<App> logger);
}
