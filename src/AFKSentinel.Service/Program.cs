using AFKSentinel.Core.Models; // For DetectionSettings
using AFKSentinel.Service;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;

IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "AFK-Sentinel Service";
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.Configure<DetectionSettings>(hostContext.Configuration.GetSection("DetectionSettings"));
        services.AddHostedService<Worker>();
    });

var host = hostBuilder.Build();
host.Run();
