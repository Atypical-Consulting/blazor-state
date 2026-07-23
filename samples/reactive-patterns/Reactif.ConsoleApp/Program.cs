using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Reactif.ConsoleApp;
using Reactif.ConsoleApp.Services.Pipelines;
using Serilog;

var logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/reactif.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var host = new HostBuilder()
    .UseSerilog(logger)
    .ConfigureServices((_, serviceCollection) =>
    {
        serviceCollection.RegisterServices();
    })
    .Build();

using var serviceScope = host.Services.CreateScope();
var serviceProvider = serviceScope.ServiceProvider;

IDisposable? processFilesSubscription = null;

try
{
    Log.Information("Starting Reactif application");

    // Resolve and execute your ChainOfResponsibilityExample here
    serviceProvider
        .GetRequiredService<ChainOfResponsibilityExample>()
        .Execute();

    // If you have other code to run, you can do so here
    // processFilesSubscription = serviceProvider
    //     .GetRequiredService<FileProcessingOrchestrator>()
    //     .ProcessFiles();
    
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();
}
catch (Exception ex)
{
    Log.Fatal(ex, "An error occurred while running the Reactif application");
}
finally
{
    await Log.CloseAndFlushAsync();
    processFilesSubscription?.Dispose();
}
