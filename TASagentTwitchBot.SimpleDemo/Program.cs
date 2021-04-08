using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace TASagentTwitchBot.SimpleDemo
{
    public class Program
    {
        static void Main(string[] args)
        {
            //Initialize DataManagement
            BGC.IO.DataManagement.Initialize("TASagentBotDemo");

            //netsh http add urlacl url="http://+:5000/" user=everyone
            IWebHost host = WebHost
                .CreateDefaultBuilder(args)
                .UseKestrel()
                .UseUrls("http://0.0.0.0:5000")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseWebRoot("../TASagentTwitchBotCore/Pages/wwwroot")
                .UseIISIntegration()
                .UseStartup<Web.Startup>()
                .Build();

            host.StartAsync().Wait();

            TwitchBotDemoApplication application = host.Services.GetService(typeof(TwitchBotDemoApplication)) as TwitchBotDemoApplication;
            application.RunAsync().Wait();

            host.StopAsync().Wait();

            host.Dispose();
        }
    }
}
