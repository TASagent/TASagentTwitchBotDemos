﻿using System;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using TASagentTwitchBot.Core.Web;

namespace TASagentTwitchBot.SimpleDemo.Web
{
    public class Startup : StartupCore
    {
        public Startup(IConfiguration configuration)
            : base(configuration)
        {
        }

        //Override 
        protected override void ConfigureDatabases(IServiceCollection services)
        {
            //Register new database
            services.AddSingleton<Database.DatabaseContext>();

            //Register new database to be served for required BaseDatabaseContext
            services.AddSingleton<Core.Database.BaseDatabaseContext>(x => x.GetRequiredService<Database.DatabaseContext>());
        }

        protected override void ConfigureCustomServices(IServiceCollection services)
        {
            services
                .AddSingleton<TwitchBotDemoApplication>()
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

            //Controller Overlay
            services
                .AddSingleton<Plugin.ControllerSpy.IControllerManager, Plugin.ControllerSpy.ControllerManager>();

            //Add ControllerSpy Controllers
            Assembly assembly = typeof(Plugin.ControllerSpy.Web.Controllers.ControllerSpyController).Assembly;
            services.AddMvc().AddApplicationPart(assembly).AddControllersAsServices();
        }

        protected override void BuildCustomEndpointRoutes(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHub<Plugin.ControllerSpy.Web.Hubs.ControllerSpyHub>("/Hubs/ControllerSpy");
        }
    }
}