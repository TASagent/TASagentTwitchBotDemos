using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

using TASagentTwitchBot.Core.Extensions;
using TASagentTwitchBot.Core.Web;
using TASagentTwitchBot.Plugin.ControllerSpy.Web;

//Initialize DataManagement
BGC.IO.DataManagement.Initialize("TASagentBotEmoteEffectsDemo");

//
// Define and register services
//

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.WebHost
    .UseKestrel()
    .UseUrls("http://0.0.0.0:5000");

IMvcBuilder mvcBuilder = builder.Services.GetMvcBuilder();

//Register ControllerSpy for inclusion
mvcBuilder.AddControllerSpyControllerAssembly();

//Register Core Controllers (with potential exclusions) 
mvcBuilder.RegisterControllersWithoutFeatures("Midi", "TTS", "Audio", "Notifications");

//Add SignalR for Hubs
builder.Services.AddSignalR();

//Custom Database
builder.Services
    .AddDbContext<TASagentTwitchBot.EmoteEffectsDemo.Database.DatabaseContext>();

//Register custom database to be served for BaseDatabaseContext
builder.Services
    .AddScoped<TASagentTwitchBot.Core.Database.BaseDatabaseContext>(x => x.GetRequiredService<TASagentTwitchBot.EmoteEffectsDemo.Database.DatabaseContext>());

//Core Agnostic Systems
builder.Services
    .AddSingleton<TASagentTwitchBot.Core.Config.BotConfiguration>(TASagentTwitchBot.Core.Config.BotConfiguration.GetConfig())
    .AddSingleton<TASagentTwitchBot.Core.ICommunication, TASagentTwitchBot.Core.CommunicationHandler>()
    .AddSingleton<TASagentTwitchBot.Core.View.IConsoleOutput, TASagentTwitchBot.Core.View.BasicView>()
    .AddSingleton<TASagentTwitchBot.Core.ErrorHandler>()
    .AddSingleton<TASagentTwitchBot.Core.ApplicationManagement>()
    .AddSingleton<TASagentTwitchBot.Core.Chat.ChatLogger>()
    .AddSingleton<TASagentTwitchBot.Core.IMessageAccumulator, TASagentTwitchBot.Core.MessageAccumulator>();

builder.Services
    .AddSingleton<TASagentTwitchBot.Core.IConfigurator, TASagentTwitchBot.EmoteEffectsDemo.EmoteEffectsDemoConfigurator>();

//Core Twitch Systems
builder.Services
    .AddSingleton<TASagentTwitchBot.Core.API.Twitch.HelixHelper>()
    .AddSingleton<TASagentTwitchBot.Core.API.Twitch.IBotTokenValidator, TASagentTwitchBot.Core.API.Twitch.BotTokenValidator>()
    .AddSingleton<TASagentTwitchBot.Core.API.Twitch.IBroadcasterTokenValidator, TASagentTwitchBot.Core.API.Twitch.BroadcasterTokenValidator>()
    .AddSingleton<TASagentTwitchBot.Core.Database.IUserHelper, TASagentTwitchBot.Core.Database.UserHelper>();

//Core Twitch Chat Systems
builder.Services
    .AddSingleton<TASagentTwitchBot.Core.IRC.IrcClient>()
    .AddSingleton<TASagentTwitchBot.Core.IRC.IIRCLogger, TASagentTwitchBot.Core.IRC.IRCLogger>()
    .AddSingleton<TASagentTwitchBot.Core.IRC.INoticeHandler, TASagentTwitchBot.Core.IRC.NoticeHandler>()
    .AddSingleton<TASagentTwitchBot.Core.Chat.IChatMessageHandler, TASagentTwitchBot.Core.Chat.ChatMessageHandler>();

//Notification Stubs
builder.Services
    .AddSingleton<TASagentTwitchBot.Core.Notifications.ActivityProviderStubs>();

builder.Services
    .AddSingleton<TASagentTwitchBot.Core.Notifications.ITTSHandler>(x => x.GetRequiredService<TASagentTwitchBot.Core.Notifications.ActivityProviderStubs>())
    .AddSingleton<TASagentTwitchBot.Core.Notifications.ISubscriptionHandler>(x => x.GetRequiredService<TASagentTwitchBot.Core.Notifications.ActivityProviderStubs>())
    .AddSingleton<TASagentTwitchBot.Core.Notifications.ICheerHandler>(x => x.GetRequiredService<TASagentTwitchBot.Core.Notifications.ActivityProviderStubs>())
    .AddSingleton<TASagentTwitchBot.Core.Notifications.IRaidHandler>(x => x.GetRequiredService<TASagentTwitchBot.Core.Notifications.ActivityProviderStubs>())
    .AddSingleton<TASagentTwitchBot.Core.Notifications.IGiftSubHandler>(x => x.GetRequiredService<TASagentTwitchBot.Core.Notifications.ActivityProviderStubs>())
    .AddSingleton<TASagentTwitchBot.Core.Notifications.IFollowerHandler>(x => x.GetRequiredService<TASagentTwitchBot.Core.Notifications.ActivityProviderStubs>());

//Core Emote Effects System
builder.Services
    .AddSingleton<TASagentTwitchBot.Core.API.BTTV.BTTVHelper>()
    .AddSingleton<TASagentTwitchBot.Core.EmoteEffects.EmoteEffectConfiguration>(TASagentTwitchBot.Core.EmoteEffects.EmoteEffectConfiguration.GetConfig())
    .AddSingleton<TASagentTwitchBot.Core.EmoteEffects.IEmoteEffectListener, TASagentTwitchBot.Core.EmoteEffects.EmoteEffectListener>()
    .AddSingleton<TASagentTwitchBot.Core.Commands.ICommandContainer, TASagentTwitchBot.Core.EmoteEffects.EmoteEffectSystem>();

//Core Timer System
builder.Services.AddSingleton<TASagentTwitchBot.Core.Timer.ITimerManager, TASagentTwitchBot.Core.Timer.TimerManager>();

//Core Controller Overlay
builder.Services.RegisterControllerSpyServices();

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

using WebApplication app = builder.Build();

//Handle forwarding properly
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

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

//Core Controllerspy Assets
app.UseCoreLibraryContent("TASagentTwitchBot.Plugin.ControllerSpy");

//Authentication Middleware
app.UseMiddleware<TASagentTwitchBot.Core.Web.Middleware.AuthCheckerMiddleware>();

//Map all Core Non-excluded controllers
app.MapControllers();

//Core Control Page Hub
app.MapHub<TASagentTwitchBot.Core.Web.Hubs.MonitorHub>("/Hubs/Monitor");

//Core Timer Hub
app.MapHub<TASagentTwitchBot.Core.Web.Hubs.TimerHub>("/Hubs/Timer");

//Core Emote Effect Overlay Hub
app.MapHub<TASagentTwitchBot.Core.Web.Hubs.EmoteHub>("/Hubs/Emote");

//Core ControllerSpy Endpoints
app.RegisterControllerSpyEndpoints();


await app.StartAsync();

//
// Update Database with new migrations
//

using (IServiceScope serviceScope = app.Services.GetService<IServiceScopeFactory>()!.CreateScope())
{
    TASagentTwitchBot.EmoteEffectsDemo.Database.DatabaseContext context = serviceScope.ServiceProvider!.GetRequiredService<TASagentTwitchBot.EmoteEffectsDemo.Database.DatabaseContext>();
    context.Database.Migrate();
}

//
// Construct and run Configurator
//


TASagentTwitchBot.Core.ICommunication communication = app.Services.GetRequiredService<TASagentTwitchBot.Core.ICommunication>();
TASagentTwitchBot.Core.IConfigurator configurator = app.Services.GetRequiredService<TASagentTwitchBot.Core.IConfigurator>();

app.Services.GetRequiredService<TASagentTwitchBot.Core.View.IConsoleOutput>();
app.Services.GetRequiredService<TASagentTwitchBot.Core.Chat.ChatLogger>();

bool configurationSuccessful = await configurator.VerifyConfigured();

if (!configurationSuccessful)
{
    communication.SendErrorMessage($"Configuration unsuccessful. Aborting.");

    await app.StopAsync();
    await Task.Delay(15_000);
    return;
}

//
// Construct required components and run
//
communication.SendDebugMessage("*** Starting Up ***");

TASagentTwitchBot.Core.ErrorHandler errorHandler = app.Services.GetRequiredService<TASagentTwitchBot.Core.ErrorHandler>();
TASagentTwitchBot.Core.ApplicationManagement applicationManagement = app.Services.GetRequiredService<TASagentTwitchBot.Core.ApplicationManagement>();

TASagentTwitchBot.Core.API.Twitch.IBotTokenValidator botTokenValidator = app.Services.GetRequiredService<TASagentTwitchBot.Core.API.Twitch.IBotTokenValidator>();
TASagentTwitchBot.Core.API.Twitch.IBroadcasterTokenValidator broadcasterTokenValidator = app.Services.GetRequiredService<TASagentTwitchBot.Core.API.Twitch.IBroadcasterTokenValidator>();

app.Services.GetRequiredService<TASagentTwitchBot.Core.Commands.CommandSystem>();
app.Services.GetRequiredService<TASagentTwitchBot.Core.IMessageAccumulator>();
app.Services.GetRequiredService<TASagentTwitchBot.Core.IRC.IrcClient>();

//Kick off Validators
botTokenValidator.RunValidator();
broadcasterTokenValidator.RunValidator();

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
