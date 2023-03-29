using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz.Spi;

namespace UpdateHostsService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var logger = services.GetRequiredService<ILogger<Program>>();

                try
                {
                    var hostsScheduler = services.GetRequiredService<HostsScheduler>();
                    hostsScheduler.RemoveUnusedSectionsFromHostsFile();
                    await hostsScheduler.ScheduleJobs();
                    logger.LogInformation("HostsScheduler started successfully.");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "An error occurred while starting the HostsScheduler.");
                }
            }

            await host.RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<HostsScheduler>();
                    services.AddSingleton<HostsUpdaterJob>();
                    services.AddSingleton<IJobFactory, ServiceProviderJobFactory>();
                });


    }
}

