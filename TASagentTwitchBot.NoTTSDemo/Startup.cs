using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using TASagentTwitchBot.Core.Web;
using TASagentTwitchBot.Plugin.ControllerSpy.Web;

namespace TASagentTwitchBot.NoTTSDemo
{
    public class Startup : Core.StartupCore
    {
        public Startup(IConfiguration configuration)
            : base(configuration)
        {
        }

        protected override void ConfigureAddCustomAssemblies(IMvcBuilder builder) =>
            builder.AddControllerSpyControllerAssembly();

        protected override string[] GetExcludedFeatures() =>
            new string[] { "TTS" };

        protected override void ConfigureDatabases(IServiceCollection services)
        {
            //Register new database
            services.AddSingleton<Database.DatabaseContext>();

            //Register new database to be served for required BaseDatabaseContext
            services.AddSingleton<Core.Database.BaseDatabaseContext>(x => x.GetRequiredService<Database.DatabaseContext>());
        }

        protected override void ConfigureCustomServices(IServiceCollection services)
        {
            //De-register the TTS command
            services.UnregisterImplementation<Core.TTS.TTSSystem>();

            //De-register the ITTSHandler and ITTSRenderer interfaces to guarantee they're not constructed
            services.UnregisterInterfaceAll<Core.Notifications.ITTSHandler>();
            services.UnregisterInterfaceAll<Core.TTS.ITTSRenderer>();

            //Register core application
            services.AddSingleton<NoTTSDemoApplication>();

            //Register special No-TTS ActivityProvider
            services.AddSingleton<Notifications.NoTTSActivityProvider>();

            //Make handler requests access the same instance of the CustomActivityProvider singleton
            services
                .AddSingleton<Core.Notifications.ISubscriptionHandler>(x => x.GetRequiredService<Notifications.NoTTSActivityProvider>())
                .AddSingleton<Core.Notifications.ICheerHandler>(x => x.GetRequiredService<Notifications.NoTTSActivityProvider>())
                .AddSingleton<Core.Notifications.IRaidHandler>(x => x.GetRequiredService<Notifications.NoTTSActivityProvider>())
                .AddSingleton<Core.Notifications.IGiftSubHandler>(x => x.GetRequiredService<Notifications.NoTTSActivityProvider>())
                .AddSingleton<Core.Notifications.IFollowerHandler>(x => x.GetRequiredService<Notifications.NoTTSActivityProvider>());

            //Controller Overlay
            services.RegisterControllerSpyServices();

            //Swapping out view for basic
            services.AddSingleton<Core.View.IConsoleOutput, Core.View.BasicView>();
        }

        protected override void BuildCustomEndpointRoutes(IEndpointRouteBuilder endpoints)
        {
            endpoints.RegisterControllerSpyEndpoints();
        }

        protected override void ConfigureCustomStaticFilesSupplement(IApplicationBuilder app, IWebHostEnvironment env)
        {
            UseCoreLibraryContent(app, env, "TASagentTwitchBot.Plugin.ControllerSpy");
        }
    }
}
