using Microsoft.AspNetCore.HttpOverrides;

using TASagentTwitchBot.Core.Extensions;
using TASagentTwitchBot.Core.Web;

//Initialize DataManagement
BGC.IO.DataManagement.Initialize("TASagentBotFilesTestDemo");

//
// Define and register services
//

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.WebHost
    .UseKestrel()
    .UseUrls("http://0.0.0.0:5000");

IMvcBuilder mvcBuilder = builder.Services.GetMvcBuilder();

//Register Core Controllers (with potential exclusions) 
mvcBuilder.RegisterControllersWithoutFeatures("TTS", "Overlay", "Notifications", "Database", "Audio");

//Add SignalR for Hubs
builder.Services.AddSignalR();

//Core Agnostic Systems
builder.Services
    .AddTASSingleton(TASagentTwitchBot.Core.Config.BotConfiguration.GetConfig())
    .AddTASSingleton<TASagentTwitchBot.Core.CommunicationHandler>()
    .AddTASSingleton<TASagentTwitchBot.Core.View.BasicView>()
    .AddTASSingleton<TASagentTwitchBot.Core.ErrorHandler>()
    .AddTASSingleton<TASagentTwitchBot.Core.ApplicationManagement>()
    .AddTASSingleton<TASagentTwitchBot.Core.MessageAccumulator>();

//Routing
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

//
// Finished defining services
// Construct application
//

using WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseAuthorization();
app.UseDefaultFiles();


//Config Directory Web Assets - overriding all content
app.UseDocumentsOverrideContent();

//Custom Web Assets - overriding Core wwwroot
app.UseStaticFiles();

//Core Web Assets
app.UseCoreLibraryContent("TASagentTwitchBot.Core");

//Authentication Middleware
app.UseMiddleware<TASagentTwitchBot.Core.Web.Middleware.AuthCheckerMiddleware>();

//Map all Core Non-excluded controllers
app.MapControllers();

//Core Control Page Hub
app.MapHub<TASagentTwitchBot.Core.Web.Hubs.MonitorHub>("/Hubs/Monitor");

await app.StartAsync();


//
// Construct required components and run
//

TASagentTwitchBot.Core.ICommunication communication = app.Services.GetRequiredService<TASagentTwitchBot.Core.ICommunication>();

app.Services.GetRequiredService<TASagentTwitchBot.Core.View.IConsoleOutput>();

TASagentTwitchBot.Core.ErrorHandler errorHandler = app.Services.GetRequiredService<TASagentTwitchBot.Core.ErrorHandler>();
TASagentTwitchBot.Core.ApplicationManagement applicationManagement = app.Services.GetRequiredService<TASagentTwitchBot.Core.ApplicationManagement>();

communication.SendDebugMessage("*** Starting Files Test Application ***");
communication.SendDebugMessage("Now check out http://localhost:5000/");

foreach (TASagentTwitchBot.Core.IStartupListener startupListener in app.Services.GetServices<TASagentTwitchBot.Core.IStartupListener>())
{
    startupListener.NotifyStartup();
}

//
// Wait for signal to end application
//

try
{
    await applicationManagement.WaitForEndAsync();
}
catch (Exception ex)
{
    errorHandler.LogSystemException(ex);
}

//
// Stop webhost
//

await app.StopAsync();