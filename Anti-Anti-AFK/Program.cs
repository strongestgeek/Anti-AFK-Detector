using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MouseMonitorService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService(options =>
                {
                    options.ServiceName = "Mouse Activity Monitor";
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<MouseMonitorWorker>();
                    services.AddApplicationInsightsTelemetryWorkerService();
                });
    }
}