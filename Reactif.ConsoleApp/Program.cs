using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Reactif.ConsoleApp.Run;
using Serilog;

var logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level}] {Message}{NewLine}{Exception}")
    .CreateLogger();

var host = new HostBuilder()
    .UseSerilog(logger)
    .ConfigureServices((_, services) =>
    {
        Startup.ConfigureServices(services);
        services.AddHostedService<App>();
    })
    .Build();

await host.RunAsync();