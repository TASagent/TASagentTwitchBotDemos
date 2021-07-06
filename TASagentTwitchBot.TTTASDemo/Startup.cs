using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using TASagentTwitchBot.Plugin.TTTAS.Web;

namespace TASagentTwitchBot.TTTASDemo
{
    public class Startup : Core.StartupCore
    {
        public Startup(IConfiguration configuration)
            : base(configuration)
        {
        }

        protected override string[] GetExcludedFeatures() => new[] { "TTS", "Overlay", "Midi" };

        protected override void ConfigureDatabases(IServiceCollection services)
        {
            //Register new database
            services.AddDbContext<Database.DatabaseContext>();

            //Register new database to be served for required BaseDatabaseContext
            services.AddScoped<Core.Database.BaseDatabaseContext>(x => x.GetRequiredService<Database.DatabaseContext>());
        }

        protected override void SetupDatabase(IApplicationBuilder app)
        {
            using IServiceScope serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope();
            Database.DatabaseContext context = serviceScope.ServiceProvider.GetRequiredService<Database.DatabaseContext>();
            context.Database.Migrate();
        }

        protected override void ConfigureAddCustomAssemblies(IMvcBuilder builder)
        {
            //Plugins
            builder.AddTTTASAssembly();
        }

        //Override Core Service registration because of extensive cutting of features
        protected override void ConfigureCoreServices(IServiceCollection services)
        {
            //Construct or load BotConfiguration
            services
                .AddSingleton<Core.Config.BotConfiguration>(Core.Config.BotConfiguration.GetConfig());

            services
                .AddSingleton<Core.ErrorHandler>()
                .AddSingleton<Core.ApplicationManagement>()
                .AddSingleton<Core.IRC.IrcClient>()
                .AddSingleton<Core.API.Twitch.HelixHelper>()
                .AddSingleton<Core.PubSub.PubSubClient>();

            services
                .AddSingleton<Core.API.Twitch.IBotTokenValidator, Core.API.Twitch.BotTokenValidator>()
                .AddSingleton<Core.API.Twitch.IBroadcasterTokenValidator, Core.API.Twitch.BroadcasterTokenValidator>();


            services
                .AddSingleton<Core.ICommunication, Core.CommunicationHandler>()
                .AddSingleton<Core.IMessageAccumulator, Core.MessageAccumulator>()
                .AddSingleton<Core.Notifications.IActivityDispatcher, Core.Notifications.ActivityDispatcher>()
                .AddSingleton<Core.Audio.IAudioPlayer, Core.Audio.AudioPlayer>()
                .AddSingleton<Core.Audio.IMicrophoneHandler, Core.Audio.MicrophoneHandler>()
                .AddSingleton<Core.Audio.ISoundEffectSystem, Core.Audio.SoundEffectSystem>()
                .AddSingleton<Core.Audio.Effects.IAudioEffectSystem, Core.Audio.Effects.AudioEffectSystem>()
                .AddSingleton<Core.Chat.IChatMessageHandler, Chat.ChatMessageSimpleHandler>()
                .AddSingleton<Core.View.IConsoleOutput, View.TTTASBasicView>()
                .AddSingleton<Core.IRC.INoticeHandler, IRC.IRCNoticeIgnorer>()
                .AddSingleton<Core.IRC.IIRCLogger, IRC.IRCNonLogger>()
                .AddSingleton<Core.PubSub.IRedemptionSystem, Core.PubSub.RedemptionSystem>()
                .AddSingleton<Core.Database.IUserHelper, Core.Database.UserHelper>();

            //Returns the address that websubs should be directed at
            //Replace this with a custom class if you're behind a proxy or have a domain
            services.AddSingleton<Core.Config.IExternalWebAccessConfiguration, Core.Config.ExternalWebAccessConfiguration>();
        }

        //Don't need WebSub system
        protected override void ConfigureCoreWebSubServices(IServiceCollection services) { }

        protected override void ConfigureCoreCommandServices(IServiceCollection services)
        {
            services.AddSingleton<Core.Commands.CommandSystem>();

            services
                .AddSingleton<Core.Commands.ICommandContainer, Core.Commands.SystemCommandSystem>()
                .AddSingleton<Core.Commands.ICommandContainer, Core.Commands.PermissionSystem>();
        }

        protected override void ConfigureCustomServices(IServiceCollection services)
        {
            services.AddSingleton<TTTASDemoApplication>();

            services
                .AddSingleton<Core.IConfigurator, TTTASConfigurator>();


            services
                .AddSingleton<Core.Audio.Effects.IAudioEffectProvider, Core.Audio.Effects.ChorusEffectProvider>()
                .AddSingleton<Core.Audio.Effects.IAudioEffectProvider, Core.Audio.Effects.FrequencyModulationEffectProvider>()
                .AddSingleton<Core.Audio.Effects.IAudioEffectProvider, Core.Audio.Effects.FrequencyShiftEffectProvider>()
                .AddSingleton<Core.Audio.Effects.IAudioEffectProvider, Core.Audio.Effects.NoiseVocoderEffectProvider>()
                .AddSingleton<Core.Audio.Effects.IAudioEffectProvider, Core.Audio.Effects.PitchShiftEffectProvider>()
                .AddSingleton<Core.Audio.Effects.IAudioEffectProvider, Core.Audio.Effects.ReverbEffectProvider>();

            services.RegisterTTTASServices();

            //Override the Text-To-TAS AudioHandler with our custom audio-only version
            services.AddSingleton<Plugin.TTTAS.ITTTASHandler, TTTASAudioHandler>();
        }

        protected override void BuildCoreEndpointRoutes(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHub<Core.Web.Hubs.MonitorHub>("/Hubs/Monitor");
        }

        protected override void BuildCustomEndpointRoutes(IEndpointRouteBuilder endpoints)
        {
            //Plugins
            endpoints.RegisterTTTASEndpoints();
        }

        protected override void ConfigureCustomStaticFilesSupplement(IApplicationBuilder app, IWebHostEnvironment env)
        {
            //Plugins
            UseCoreLibraryContent(app, env, "TASagentTwitchBot.Plugin.TTTAS");
        }

        protected override void ConfigureCoreMiddleware(IApplicationBuilder app)
        {
            app.UseMiddleware<Core.Web.Middleware.AuthCheckerMiddleware>();
        }

        protected override void ConstructCoreSingletons(IServiceProvider serviceProvider)
        {
            //Make sure required services are constructed
            serviceProvider.GetRequiredService<Core.View.IConsoleOutput>();
            serviceProvider.GetRequiredService<Core.Commands.CommandSystem>();
        }
    }
}
