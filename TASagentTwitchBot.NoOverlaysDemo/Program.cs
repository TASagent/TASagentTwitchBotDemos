using System;
using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace TASagentTwitchBot.NoOverlaysDemo
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
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseWebRoot("../TASagentTwitchBotCore/Pages/wwwroot")
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.StartAsync().Wait();

            NoOverlaysDemoApplication application = host.Services.GetService(typeof(NoOverlaysDemoApplication)) as NoOverlaysDemoApplication;
            application.RunAsync().Wait();

            host.StopAsync().Wait();

            host.Dispose();
        }
    }
}
