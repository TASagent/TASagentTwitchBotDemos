using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace TASagentTwitchBot.FilesTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //Initialize DataManagement
            BGC.IO.DataManagement.Initialize("TASagentBotDemo");

            using IWebHost host = WebHost
                .CreateDefaultBuilder(args)
                .UseKestrel()
                .UseUrls("http://0.0.0.0:5000")
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.StartAsync().Wait();

            FilesTestApplication application = host.Services.GetService(typeof(FilesTestApplication)) as FilesTestApplication;
            application.RunAsync().Wait();

            host.StopAsync().Wait();
        }
    }
}
