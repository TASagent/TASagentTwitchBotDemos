using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace TASagentTwitchBot.NoTTSDemo
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

            NoTTSDemoApplication application = host.Services.GetService(typeof(NoTTSDemoApplication)) as NoTTSDemoApplication;
            application.RunAsync().Wait();

            host.StopAsync().Wait();

            host.Dispose();
        }
    }
}
