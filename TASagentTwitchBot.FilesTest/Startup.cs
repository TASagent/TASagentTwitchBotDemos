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

namespace TASagentTwitchBot.FilesTest
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
            services.RegisterControllersWithoutFeatures("TTS", "Overlay", "Notifications", "Database", "Audio");

#if DEBUG
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TwitchBotApplication", Version = "v1" });
            });
#endif

            services.AddSignalR();

            services
                .AddSingleton<Core.Config.BotConfiguration>(Core.Config.BotConfiguration.GetConfig());

            services
                .AddSingleton<FilesTestApplication>()
                .AddSingleton<Core.ErrorHandler>()
                .AddSingleton<Core.ApplicationManagement>();

            services
                .AddSingleton<Core.ICommunication, Core.CommunicationHandler>()
                .AddSingleton<Core.IMessageAccumulator, Core.MessageAccumulator>()
                .AddSingleton<Core.View.IConsoleOutput, Core.View.BasicView>();
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

            //Register Overriding wwwroot
            app.UseDefaultFiles();
            app.UseStaticFiles();

            //Register TASagentTwitchBot.Core Content
            app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(GetLibraryContentPath("TASagentTwitchBot.Core", env)),
                RequestPath = ""
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(GetLibraryContentPath("TASagentTwitchBot.Core", env)),
                RequestPath = ""
            });


            app.UseMiddleware<Core.Web.Middleware.AuthCheckerMiddleware>();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<Core.Web.Hubs.MonitorHub>("/Hubs/Monitor");
            });

            app.ApplicationServices.GetRequiredService<Core.View.IConsoleOutput>();
        }

        private static string GetLibraryContentPath(string libraryName, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                return Path.Combine(Directory.GetParent(env.ContentRootPath).FullName, "TASagentTwitchBotCore", libraryName, "wwwroot");
            }
            else
            {
                return Path.Combine(env.WebRootPath, "_content", libraryName);
            }
        }
    }
}
