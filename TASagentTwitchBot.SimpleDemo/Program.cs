using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace TASagentTwitchBot.SimpleDemo
{
    public class Program
    {
        static void Main(string[] args)
        {
            //Initialize DataManagement
            BGC.IO.DataManagement.Initialize("TASagentBotDemo");

            using IWebHost host = WebHost
                .CreateDefaultBuilder(args)
                .UseKestrel()
                .UseUrls("http://0.0.0.0:5000", $"http://0.0.0.0:{Core.Web.Middleware.WebSubHandlerMiddleware.WEBSUB_PORT}")
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.StartAsync().Wait();

            Core.IConfigurator configurator = host.Services.GetService(typeof(Core.IConfigurator)) as Core.IConfigurator;

            Task<bool> configurationSuccessful = configurator.VerifyConfigured();
            configurationSuccessful.Wait();

            if (configurationSuccessful.Result)
            {
                SimpleDemoApplication application = host.Services.GetService(typeof(SimpleDemoApplication)) as SimpleDemoApplication;
                application.RunAsync().Wait();
            }
            else
            {
                Core.ICommunication communication = host.Services.GetService(typeof(Core.ICommunication)) as Core.ICommunication;
                communication.SendErrorMessage($"Configuration unsuccessful.  Aborting.");
            }

            host.StopAsync().Wait();

            if (!configurationSuccessful.Result)
            {
                Environment.Exit(1);
            }
        }
    }
}
