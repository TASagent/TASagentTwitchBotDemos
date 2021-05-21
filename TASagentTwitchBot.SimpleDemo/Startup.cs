using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using TASagentTwitchBot.Plugin.ControllerSpy.Web;
using TASagentTwitchBot.Plugin.TTTAS.Web;

namespace TASagentTwitchBot.SimpleDemo
{
    public class Startup : Core.StartupCore
    {
        public Startup(IConfiguration configuration)
            : base(configuration)
        {
        }

        protected override void ConfigureAddCustomAssemblies(IMvcBuilder builder)
        {
            //Plugins
            builder.AddControllerSpyControllerAssembly();
            builder.AddTTTASAssembly();
        }

        protected override void ConfigureDatabases(IServiceCollection services)
        {
            //Register new database
            services.AddDbContext<Database.DatabaseContext>();

            //Register new database to be served for required BaseDatabaseContext
            services.AddScoped<Core.Database.BaseDatabaseContext>(x => x.GetRequiredService<Database.DatabaseContext>());
        }

        protected override void ConfigureCustomServices(IServiceCollection services)
        {
            services
                .AddSingleton<SimpleDemoApplication>()
                .AddSingleton<Notifications.CustomActivityProvider>();

            //Register new PointSpender Redemption container so that it can be invoked by command system
            services.AddSingleton<PointsSpender.IPointSpenderHandler, PointsSpender.PointSpenderHandler>();

            //Register each Redemption container so they are returned with requests for all redemption containers
            services.AddSingleton<Core.PubSub.IRedemptionContainer>( x=> x.GetRequiredService<PointsSpender.IPointSpenderHandler>());

            //Register new commands
            services
                .AddSingleton<Core.Commands.ICommandContainer, Commands.UpTimeSystem>()
                .AddSingleton<Core.Commands.ICommandContainer, Commands.TestCommandSystem>()
                .AddSingleton<Core.Commands.ICommandContainer, PointsSpender.PointsSpenderSystem>();

            //Make handler requests access the same instance of the CustomActivityProvider singleton
            services
                .AddSingleton<Core.Notifications.ISubscriptionHandler>(x => x.GetRequiredService<Notifications.CustomActivityProvider>())
                .AddSingleton<Core.Notifications.ICheerHandler>(x => x.GetRequiredService<Notifications.CustomActivityProvider>())
                .AddSingleton<Core.Notifications.IRaidHandler>(x => x.GetRequiredService<Notifications.CustomActivityProvider>())
                .AddSingleton<Core.Notifications.IGiftSubHandler>(x => x.GetRequiredService<Notifications.CustomActivityProvider>())
                .AddSingleton<Core.Notifications.IFollowerHandler>(x => x.GetRequiredService<Notifications.CustomActivityProvider>())
                .AddSingleton<Core.Notifications.ITTSHandler>(x => x.GetRequiredService<Notifications.CustomActivityProvider>());

            //Plugins
            services.RegisterControllerSpyServices();
            services.RegisterTTTASServices();
        }

        protected override void BuildCustomEndpointRoutes(IEndpointRouteBuilder endpoints)
        {
            //Plugins
            endpoints.RegisterControllerSpyEndpoints();
            endpoints.RegisterTTTASEndpoints();
        }

        protected override void ConfigureCustomStaticFilesSupplement(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //Plugins
            UseCoreLibraryContent(app, env, "TASagentTwitchBot.Plugin.ControllerSpy");
            UseCoreLibraryContent(app, env, "TASagentTwitchBot.Plugin.TTTAS");
        }
    }
}
