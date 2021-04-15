using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

using TASagentTwitchBot.Core.Web;

namespace TASagentTwitchBot.BasicMicController
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.RegisterControllersWithoutFeatures("TTS", "Overlay", "Notifications", "Database");

#if DEBUG
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TwitchBotApplication", Version = "v1" });
            });
#endif

            services.AddSignalR();


            services
                .AddSingleton<BasicMicApplication>()
                .AddSingleton<Core.ErrorHandler>()
                .AddSingleton<Core.ApplicationManagement>()
                .AddSingleton<Core.Audio.MidiKeyboardHandler>();

            services
                .AddSingleton<Core.IConfigurator, AudioConfigurator>();

            services
                .AddSingleton<Core.ICommunication, Core.CommunicationHandler>()
                .AddSingleton<Core.IMessageAccumulator, Core.MessageAccumulator>()
                .AddSingleton<Core.Config.IBotConfigContainer, Core.Config.BotConfigContainer>()
                .AddSingleton<Core.Audio.IAudioPlayer, Core.Audio.AudioPlayer>()
                .AddSingleton<Core.Audio.IMicrophoneHandler, Core.Audio.MicrophoneHandler>()
                .AddSingleton<Core.Audio.ISoundEffectSystem, Core.Audio.SoundEffectSystem>()
                .AddSingleton<Core.Audio.Effects.IAudioEffectSystem, Core.Audio.Effects.AudioEffectSystem>()
                .AddSingleton<Core.View.IConsoleOutput, Core.View.BasicView>();

            services
                .AddSingleton<Core.Audio.Effects.IAudioEffectProvider, Core.Audio.Effects.ChorusEffectProvider>()
                .AddSingleton<Core.Audio.Effects.IAudioEffectProvider, Core.Audio.Effects.FrequencyModulationEffectProvider>()
                .AddSingleton<Core.Audio.Effects.IAudioEffectProvider, Core.Audio.Effects.FrequencyShiftEffectProvider>()
                .AddSingleton<Core.Audio.Effects.IAudioEffectProvider, Core.Audio.Effects.NoiseVocoderEffectProvider>()
                .AddSingleton<Core.Audio.Effects.IAudioEffectProvider, Core.Audio.Effects.PitchShiftEffectProvider>()
                .AddSingleton<Core.Audio.Effects.IAudioEffectProvider, Core.Audio.Effects.ReverbEffectProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
#if DEBUG
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TwitchBotApplication v1"));
#endif
            }

            app.UseRouting();
            app.UseAuthorization();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            if (env.IsDevelopment())
            {
                app.UseDefaultFiles(new DefaultFilesOptions
                {
                    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Path.Combine(Directory.GetParent(env.ContentRootPath).FullName, "TASagentTwitchBotCore", "TASagentTwitchBot.Core", "wwwroot")),
                    RequestPath = ""
                });

                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Path.Combine(Directory.GetParent(env.ContentRootPath).FullName, "TASagentTwitchBotCore", "TASagentTwitchBot.Core", "wwwroot")),
                    RequestPath = ""
                });
            }
            else
            {
                app.UseDefaultFiles(new DefaultFilesOptions
                {
                    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Path.Combine(env.WebRootPath, "_content", "TASagentTwitchBot.Core")),
                    RequestPath = ""
                });

                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Path.Combine(env.WebRootPath, "_content", "TASagentTwitchBot.Core")),
                    RequestPath = ""
                });
            }

            app.UseMiddleware<Core.Web.Middleware.AuthCheckerMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<Core.Web.Hubs.MonitorHub>("/Hubs/Monitor");
            });

            app.ApplicationServices.GetRequiredService<Core.Config.IBotConfigContainer>().Initialize();
            app.ApplicationServices.GetRequiredService<Core.View.IConsoleOutput>();
        }
    }
}
