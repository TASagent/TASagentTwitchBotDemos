using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

using TASagentTwitchBot.Core.Extensions;
using TASagentTwitchBot.Core.Web;
using TASagentTwitchBot.Plugin.TTTAS.Web;

//Initialize DataManagement
BGC.IO.DataManagement.Initialize("TASagentBotTTTASDemo");

//
// Define and register services
//

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.WebHost
    .UseKestrel()
    .UseUrls("http://0.0.0.0:5000");

IMvcBuilder mvcBuilder = builder.Services.GetMvcBuilder();

//Register TTTAS Assembly for inclusion
mvcBuilder.AddTTTASAssembly();

//Register Core Controllers (with potential exclusions) 
mvcBuilder.RegisterControllersWithoutFeatures("TTS", "Overlay", "Midi");

//Add SignalR for Hubs
builder.Services.AddSignalR();

//Custom Database
builder.Services.AddTASDbContext<TASagentTwitchBot.TTTASDemo.Database.DatabaseContext>();

//Core Agnostic Systems
builder.Services
    .AddTASSingleton(TASagentTwitchBot.Core.Config.BotConfiguration.GetConfig(GetDefaultBotConfig()))
    .AddTASSingleton<TASagentTwitchBot.Core.CommunicationHandler>()
    .AddTASSingleton<TASagentTwitchBot.Core.ErrorHandler>()
    .AddTASSingleton<TASagentTwitchBot.Core.ApplicationManagement>()
    .AddTASSingleton<TASagentTwitchBot.Core.MessageAccumulator>();

//Custom Agnostic Systems
builder.Services
    .AddTASSingleton<TASagentTwitchBot.TTTASDemo.TTTASConfigurator>()
    .AddTASSingleton<TASagentTwitchBot.TTTASDemo.View.TTTASBasicView>();


//Core Twitch Systems
builder.Services
    .AddTASSingleton<TASagentTwitchBot.Core.API.Twitch.HelixHelper>()
    .AddTASSingleton<TASagentTwitchBot.Core.API.Twitch.BotTokenValidator>()
    .AddTASSingleton<TASagentTwitchBot.Core.API.Twitch.BroadcasterTokenValidator>()
    .AddTASSingleton<TASagentTwitchBot.Core.Database.UserHelper>();

//Core Twitch Chat Systems
builder.Services
    .AddTASSingleton<TASagentTwitchBot.Core.IRC.IrcClient>();

//Custom Twitch Chat Systems
builder.Services
    .AddTASSingleton<TASagentTwitchBot.TTTASDemo.Chat.ChatMessageSimpleHandler>()
    .AddTASSingleton<TASagentTwitchBot.TTTASDemo.IRC.IRCNonLogger>()
    .AddTASSingleton<TASagentTwitchBot.TTTASDemo.IRC.IRCNoticeIgnorer>();


//Notification System
//Core Notifications
builder.Services
    .AddTASSingleton<TASagentTwitchBot.Core.Notifications.NotificationServer>()
    .AddTASSingleton<TASagentTwitchBot.Core.Notifications.ActivityDispatcher>();

//Custom ActivityHandler
builder.Services
    .AddTASSingleton<TASagentTwitchBot.TTTASDemo.TTTASAudioHandler>();


//Core Audio System
builder.Services
    .AddTASSingleton<TASagentTwitchBot.Core.Audio.NAudioDeviceManager>()
    .AddTASSingleton<TASagentTwitchBot.Core.Audio.NAudioPlayer>()
    .AddTASSingleton<TASagentTwitchBot.Core.Audio.NAudioMicrophoneHandler>()
    .AddTASSingleton<TASagentTwitchBot.Core.Audio.SoundEffectSystem>();


//Core Audio Effects System
builder.Services
    .AddTASSingleton<TASagentTwitchBot.Core.Audio.Effects.AudioEffectSystem>()
    .AddTASSingleton<TASagentTwitchBot.Core.Audio.Effects.ChorusEffectProvider>()
    .AddTASSingleton<TASagentTwitchBot.Core.Audio.Effects.EchoEffectProvider>()
    .AddTASSingleton<TASagentTwitchBot.Core.Audio.Effects.FrequencyModulationEffectProvider>()
    .AddTASSingleton<TASagentTwitchBot.Core.Audio.Effects.FrequencyShiftEffectProvider>()
    .AddTASSingleton<TASagentTwitchBot.Core.Audio.Effects.NoiseVocoderEffectProvider>()
    .AddTASSingleton<TASagentTwitchBot.Core.Audio.Effects.PitchShiftEffectProvider>()
    .AddTASSingleton<TASagentTwitchBot.Core.Audio.Effects.ReverbEffectProvider>();

//EventSub System
builder.Services
    .AddTASSingleton<TASagentTwitchBot.Core.EventSub.EventSubWebSocketHandler>();

//Core Redemption System
builder.Services
    .AddTASSingleton<TASagentTwitchBot.Core.Redemptions.RedemptionSystem>();

//Core Timer System
builder.Services.AddTASSingleton<TASagentTwitchBot.Core.Timer.TimerManager>();

//TTTAS System
builder.Services.RegisterTTTASServices();


//Command System
//Core Commands
builder.Services
    .AddTASSingleton<TASagentTwitchBot.Core.Commands.CommandSystem>()
    .AddTASSingleton<TASagentTwitchBot.Core.Commands.SystemCommandSystem>()
    .AddTASSingleton<TASagentTwitchBot.Core.Commands.PermissionSystem>();

//Core Credit System
builder.Services
    .AddTASSingleton<TASagentTwitchBot.Core.Credit.DisabledCreditManager>();

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

//Custom Web Assets
app.UseStaticFiles();

//Core Web Assets
app.UseCoreLibraryContent("TASagentTwitchBot.Core");

//TTTAS Assets
app.UseCoreLibraryContent("TASagentTwitchBot.Plugin.TTTAS");

//Authentication Middleware
app.UseMiddleware<TASagentTwitchBot.Core.Web.Middleware.AuthCheckerMiddleware>();

//Map all Core Non-excluded controllers
app.MapControllers();

//Core Control Page Hub
app.MapHub<TASagentTwitchBot.Core.Web.Hubs.MonitorHub>("/Hubs/Monitor");


//TTTAS Endpoints
app.RegisterTTTASEndpoints();


await app.StartAsync();

//
// Update Database with new migrations
//

using (IServiceScope serviceScope = app.Services.GetService<IServiceScopeFactory>()!.CreateScope())
{
    TASagentTwitchBot.TTTASDemo.Database.DatabaseContext context = serviceScope.ServiceProvider!.GetRequiredService<TASagentTwitchBot.TTTASDemo.Database.DatabaseContext>();
    context.Database.Migrate();
}

//
// Construct and run Configurator
//

TASagentTwitchBot.Core.ICommunication communication = app.Services.GetRequiredService<TASagentTwitchBot.Core.ICommunication>();
TASagentTwitchBot.Core.IConfigurator configurator = app.Services.GetRequiredService<TASagentTwitchBot.Core.IConfigurator>();

app.Services.GetRequiredService<TASagentTwitchBot.Core.View.IConsoleOutput>();

bool configurationSuccessful = await configurator.VerifyConfigured();

if (!configurationSuccessful)
{
    communication.SendErrorMessage($"Configuration unsuccessful.  Aborting.");

    await app.StopAsync();
    await Task.Delay(15_000);
    return;
}

//
// Construct required components and run
//
communication.SendDebugMessage("*** Starting Up TTTAS Application ***");

TASagentTwitchBot.Core.ErrorHandler errorHandler = app.Services.GetRequiredService<TASagentTwitchBot.Core.ErrorHandler>();
TASagentTwitchBot.Core.ApplicationManagement applicationManagement = app.Services.GetRequiredService<TASagentTwitchBot.Core.ApplicationManagement>();

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

TASagentTwitchBot.Core.Config.BotConfiguration GetDefaultBotConfig() =>
    new TASagentTwitchBot.Core.Config.BotConfiguration()
    {
        Version = TASagentTwitchBot.Core.Config.BotConfiguration.CURRENT_VERSION,
        MicConfiguration = new TASagentTwitchBot.Core.Config.MicConfiguration() { Enabled = false },
        CommandConfiguration = new TASagentTwitchBot.Core.Config.CommandConfiguration
        {
            GlobalErrorHandlingEnabled = false,
            HelpEnabled = false
        }
    };
