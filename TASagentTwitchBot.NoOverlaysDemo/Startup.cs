using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using TASagentTwitchBot.Core.Web;

namespace TASagentTwitchBot.NoOverlaysDemo
{
    public class Startup : Core.StartupCore
    {
        public Startup(IConfiguration configuration)
            : base(configuration)
        {
        }

        protected override string[] GetExcludedFeatures() =>
            new string[] { "Overlay" };

        protected override void ConfigureDatabases(IServiceCollection services)
        {
            //Register new database
            services.AddDbContext<Database.DatabaseContext>();

            //Register new database to be served for required BaseDatabaseContext
            services.AddScoped<Core.Database.BaseDatabaseContext>(x => x.GetRequiredService<Database.DatabaseContext>());
        }

        protected override void ConfigureCustomServices(IServiceCollection services)
        {
            //De-register overlay-related services
            services
                .UnregisterImplementation<Core.Bits.CheerHelper>()
                .UnregisterImplementation<Core.Notifications.NotificationServer>()
                .UnregisterImplementation<Core.Notifications.FullActivityProvider>()
                .UnregisterImplementation<Core.Timer.TimerManager>()
                .UnregisterImplementation<Core.EmoteEffects.EmoteEffectListener>();

            //Register core application
            services.AddSingleton<NoOverlaysDemoApplication>();

            //Register special No-Overlay ActivityProvider
            services.AddSingleton<Notifications.NoOverlayActivityProvider>();

            //Make handler requests access the same instance of the CustomActivityProvider singleton
            services
                .AddSingleton<Core.Notifications.ISubscriptionHandler>(x => x.GetRequiredService<Notifications.NoOverlayActivityProvider>())
                .AddSingleton<Core.Notifications.ICheerHandler>(x => x.GetRequiredService<Notifications.NoOverlayActivityProvider>())
                .AddSingleton<Core.Notifications.IRaidHandler>(x => x.GetRequiredService<Notifications.NoOverlayActivityProvider>())
                .AddSingleton<Core.Notifications.IGiftSubHandler>(x => x.GetRequiredService<Notifications.NoOverlayActivityProvider>())
                .AddSingleton<Core.Notifications.IFollowerHandler>(x => x.GetRequiredService<Notifications.NoOverlayActivityProvider>())
                .AddSingleton<Core.Notifications.ITTSHandler>(x => x.GetRequiredService<Notifications.NoOverlayActivityProvider>());
        }

        protected override void BuildCoreEndpointRoutes(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHub<Core.Web.Hubs.MonitorHub>("/Hubs/Monitor");
        }

        protected override void ConstructCoreSingletons(IServiceProvider serviceProvider)
        {
            //Make sure required services are constructed
            serviceProvider.GetRequiredService<Core.View.IConsoleOutput>();
            serviceProvider.GetRequiredService<Core.Chat.ChatLogger>();
            serviceProvider.GetRequiredService<Core.Commands.CommandSystem>();
        }
    }
}
