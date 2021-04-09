using System;
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

            IWebHost host = WebHost
                .CreateDefaultBuilder(args)
                .UseKestrel()
                .UseUrls("http://0.0.0.0:5000")
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.StartAsync().Wait();

            SimpleDemoApplication application = host.Services.GetService(typeof(SimpleDemoApplication)) as SimpleDemoApplication;
            application.RunAsync().Wait();

            host.StopAsync().Wait();

            host.Dispose();
        }
    }
}
