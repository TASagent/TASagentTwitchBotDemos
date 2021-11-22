using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

using TASagentTwitchBot.Core.Extensions;
using TASagentTwitchBot.Core.Web;
using TASagentTwitchBot.Plugin.TTTAS.Web;

//Initialize DataManagement
BGC.IO.DataManagement.Initialize("TASagentBotDemo");

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
mvcBuilder.RegisterControllersWithoutFeatures(new[] { "TTS", "Overlay", "Midi" });

//Add SignalR for Hubs
builder.Services.AddSignalR();

//Custom Database
builder.Services
    .AddDbContext<TASagentTwitchBot.TTTASDemo.Database.DatabaseContext>();

//Register custom database to be served for BaseDatabaseContext
builder.Services
    .AddScoped<TASagentTwitchBot.Core.Database.BaseDatabaseContext>(x => x.GetRequiredService<TASagentTwitchBot.TTTASDemo.Database.DatabaseContext>());

//Core Agnostic Systems
builder.Services
    .AddSingleton<TASagentTwitchBot.Core.Config.BotConfiguration>(TASagentTwitchBot.Core.Config.BotConfiguration.GetConfig())
    .AddSingleton<TASagentTwitchBot.Core.ICommunication, TASagentTwitchBot.Core.CommunicationHandler>()
    .AddSingleton<TASagentTwitchBot.Core.ErrorHandler>()
    .AddSingleton<TASagentTwitchBot.Core.ApplicationManagement>()
    .AddSingleton<TASagentTwitchBot.Core.IMessageAccumulator, TASagentTwitchBot.Core.MessageAccumulator>();

//Custom Agnostic Systems
builder.Services
    .AddSingleton<TASagentTwitchBot.Core.IConfigurator, TASagentTwitchBot.TTTASDemo.TTTASConfigurator>()
    .AddSingleton<TASagentTwitchBot.Core.View.IConsoleOutput, TASagentTwitchBot.TTTASDemo.View.TTTASBasicView>();


//Core Twitch Systems
builder.Services
    .AddSingleton<TASagentTwitchBot.Core.API.Twitch.HelixHelper>()
    .AddSingleton<TASagentTwitchBot.Core.API.Twitch.IBotTokenValidator, TASagentTwitchBot.Core.API.Twitch.BotTokenValidator>()
    .AddSingleton<TASagentTwitchBot.Core.API.Twitch.IBroadcasterTokenValidator, TASagentTwitchBot.Core.API.Twitch.BroadcasterTokenValidator>()
    .AddSingleton<TASagentTwitchBot.Core.Database.IUserHelper, TASagentTwitchBot.Core.Database.UserHelper>();

//Core Twitch Chat Systems
builder.Services
    .AddSingleton<TASagentTwitchBot.Core.IRC.IrcClient>();

//Custom Twitch Chat Systems
builder.Services
    .AddSingleton<TASagentTwitchBot.Core.Chat.IChatMessageHandler, TASagentTwitchBot.TTTASDemo.Chat.ChatMessageSimpleHandler>()
    .AddSingleton<TASagentTwitchBot.Core.IRC.IIRCLogger, TASagentTwitchBot.TTTASDemo.IRC.IRCNonLogger>()
    .AddSingleton<TASagentTwitchBot.Core.IRC.INoticeHandler, TASagentTwitchBot.TTTASDemo.IRC.IRCNoticeIgnorer>();


//Notification System
//Core Notifications
builder.Services
    .AddSingleton<TASagentTwitchBot.Core.Notifications.NotificationServer>()
    .AddSingleton<TASagentTwitchBot.Core.Notifications.IActivityDispatcher, TASagentTwitchBot.Core.Notifications.ActivityDispatcher>();

//Core Audio System
builder.Services
    .AddSingleton<TASagentTwitchBot.Core.Audio.IAudioPlayer, TASagentTwitchBot.Core.Audio.AudioPlayer>()
    .AddSingleton<TASagentTwitchBot.Core.Audio.IMicrophoneHandler, TASagentTwitchBot.Core.Audio.MicrophoneHandler>()
    .AddSingleton<TASagentTwitchBot.Core.Audio.ISoundEffectSystem, TASagentTwitchBot.Core.Audio.SoundEffectSystem>();


//Core Audio Effects System
builder.Services
    .AddSingleton<TASagentTwitchBot.Core.Audio.Effects.IAudioEffectSystem, TASagentTwitchBot.Core.Audio.Effects.AudioEffectSystem>()
    .AddSingleton<TASagentTwitchBot.Core.Audio.Effects.IAudioEffectProvider, TASagentTwitchBot.Core.Audio.Effects.ChorusEffectProvider>()
    .AddSingleton<TASagentTwitchBot.Core.Audio.Effects.IAudioEffectProvider, TASagentTwitchBot.Core.Audio.Effects.EchoEffectProvider>()
    .AddSingleton<TASagentTwitchBot.Core.Audio.Effects.IAudioEffectProvider, TASagentTwitchBot.Core.Audio.Effects.FrequencyModulationEffectProvider>()
    .AddSingleton<TASagentTwitchBot.Core.Audio.Effects.IAudioEffectProvider, TASagentTwitchBot.Core.Audio.Effects.FrequencyShiftEffectProvider>()
    .AddSingleton<TASagentTwitchBot.Core.Audio.Effects.IAudioEffectProvider, TASagentTwitchBot.Core.Audio.Effects.NoiseVocoderEffectProvider>()
    .AddSingleton<TASagentTwitchBot.Core.Audio.Effects.IAudioEffectProvider, TASagentTwitchBot.Core.Audio.Effects.PitchShiftEffectProvider>()
    .AddSingleton<TASagentTwitchBot.Core.Audio.Effects.IAudioEffectProvider, TASagentTwitchBot.Core.Audio.Effects.ReverbEffectProvider>();

//Core PubSub System
builder.Services
    .AddSingleton<TASagentTwitchBot.Core.PubSub.PubSubClient>()
    .AddSingleton<TASagentTwitchBot.Core.PubSub.IRedemptionSystem, TASagentTwitchBot.Core.PubSub.RedemptionSystem>();

//Core Midi System
builder.Services
    .AddSingleton<TASagentTwitchBot.Core.Audio.MidiKeyboardHandler>();

//Core Emote Effects System
builder.Services
    .AddSingleton<TASagentTwitchBot.Core.API.BTTV.BTTVHelper>()
    .AddSingleton<TASagentTwitchBot.Core.EmoteEffects.EmoteEffectConfiguration>(TASagentTwitchBot.Core.EmoteEffects.EmoteEffectConfiguration.GetConfig())
    .AddSingleton<TASagentTwitchBot.Core.EmoteEffects.IEmoteEffectListener, TASagentTwitchBot.Core.EmoteEffects.EmoteEffectListener>()
    .AddSingleton<TASagentTwitchBot.Core.Commands.ICommandContainer, TASagentTwitchBot.Core.EmoteEffects.EmoteEffectSystem>();


//Core Timer System
builder.Services.AddSingleton<TASagentTwitchBot.Core.Timer.ITimerManager, TASagentTwitchBot.Core.Timer.TimerManager>();

//Core TTTAS System
builder.Services.RegisterTTTASServices();


//Override the Text-To-TAS AudioHandler with our custom audio-only version
builder.Services.UnregisterImplementation<TASagentTwitchBot.Plugin.TTTAS.TTTASFullHandler>();
builder.Services.AddSingleton<TASagentTwitchBot.Plugin.TTTAS.ITTTASHandler, TASagentTwitchBot.TTTASDemo.TTTASAudioHandler>();


//Command System
//Core Commands
builder.Services
    .AddSingleton<TASagentTwitchBot.Core.Commands.CommandSystem>()
    .AddSingleton<TASagentTwitchBot.Core.Commands.ICommandContainer, TASagentTwitchBot.Core.Commands.SystemCommandSystem>()
    .AddSingleton<TASagentTwitchBot.Core.Commands.ICommandContainer, TASagentTwitchBot.Core.Commands.PermissionSystem>();

//Routing
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
        Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
});


//
// Finished defining services
// Construct application
//

WebApplication? app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.UseAuthorization();
app.UseDefaultFiles();

//Custom Web Assets
app.UseStaticFiles();

//Core Web Assets
app.UseCoreLibraryContent("TASagentTwitchBot.Core");

//Core TTTAS Assets
app.UseCoreLibraryContent("TASagentTwitchBot.Plugin.TTTAS");

//Authentication Middleware
app.UseMiddleware<TASagentTwitchBot.Core.Web.Middleware.AuthCheckerMiddleware>();

//Map all Core Non-excluded controllers
app.MapControllers();

//Core Control Page Hub
app.MapHub<TASagentTwitchBot.Core.Web.Hubs.MonitorHub>("/Hubs/Monitor");


//Core TTTAS Endpoints
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

    Environment.Exit(1);
    return;
}

//
// Construct required components and run
//

TASagentTwitchBot.Core.ErrorHandler errorHandler = app.Services.GetRequiredService<TASagentTwitchBot.Core.ErrorHandler>();
TASagentTwitchBot.Core.ApplicationManagement applicationManagement = app.Services.GetRequiredService<TASagentTwitchBot.Core.ApplicationManagement>();

TASagentTwitchBot.Core.API.Twitch.IBotTokenValidator botTokenValidator = app.Services.GetRequiredService<TASagentTwitchBot.Core.API.Twitch.IBotTokenValidator>();
TASagentTwitchBot.Core.API.Twitch.IBroadcasterTokenValidator broadcasterTokenValidator = app.Services.GetRequiredService<TASagentTwitchBot.Core.API.Twitch.IBroadcasterTokenValidator>();

app.Services.GetRequiredService<TASagentTwitchBot.Core.Commands.CommandSystem>();
app.Services.GetRequiredService<TASagentTwitchBot.Core.Audio.IMicrophoneHandler>();
app.Services.GetRequiredService<TASagentTwitchBot.Core.IMessageAccumulator>();
app.Services.GetRequiredService<TASagentTwitchBot.Core.IRC.IrcClient>();
app.Services.GetRequiredService<TASagentTwitchBot.Core.PubSub.PubSubClient>();

try
{
    communication.SendDebugMessage("*** Starting Up TTTAS Application ***");

    //Kick off Validators
    botTokenValidator.RunValidator();
    broadcasterTokenValidator.RunValidator();
}
catch (Exception ex)
{
    errorHandler.LogFatalException(ex);
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
