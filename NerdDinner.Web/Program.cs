using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Extensions.Logging;
using NerdDinner.Web.Models;
using Microsoft.Extensions.Configuration.UserSecrets;

[assembly: UserSecretsId("nerd-dinner-8f56-55188f768881")]

namespace NerdDinner.Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = BuildWebHost(args);
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                try
                {
                    //Populate dinner sample data
                    await SampleData.InitializeNerdDinner(services);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred seeding the DB.");
                }
            }

            await host.RunAsync();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((ctx, config) =>
                {
                    var env = ctx.HostingEnvironment;
                    if (env.IsDevelopment())
                    {
                        config.AddUserSecrets<Program>();
                        // This will push telemetry data through Application Insights pipeline faster, allowing you to view results immediately.
                        config.AddApplicationInsightsSettings(developerMode: true);
                    }
                })
                .UseStartup<Startup>()
                .Build();
    }
}
