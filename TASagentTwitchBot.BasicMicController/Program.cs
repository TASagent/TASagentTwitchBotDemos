using TASagentTwitchBot.Core.Extensions;
using TASagentTwitchBot.Core.Web;
using TASagentTwitchBot.Plugin.Audio.Midi;

//Initialize DataManagement
BGC.IO.DataManagement.Initialize("TASagentBotMicControllerDemo");

//
// Define and register services
//

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.WebHost
    .UseKestrel()
    .UseUrls("http://0.0.0.0:5000");

IMvcBuilder mvcBuilder = builder.Services.GetMvcBuilder();

//Register Midi Assembly for inclusion
mvcBuilder.AddMidiAssembly();

//Register Core Controllers (with potential exclusions) 
mvcBuilder.RegisterControllersWithoutFeatures("TTS", "Overlay", "Notifications", "Database");

//Add SignalR for Hubs
builder.Services.AddSignalR();

//Core Agnostic Systems
builder.Services
    .AddSingleton<TASagentTwitchBot.Core.Config.BotConfiguration>(TASagentTwitchBot.Core.Config.BotConfiguration.GetConfig())
    .AddSingleton<TASagentTwitchBot.Core.ICommunication, TASagentTwitchBot.Core.CommunicationHandler>()
    .AddSingleton<TASagentTwitchBot.Core.View.IConsoleOutput, TASagentTwitchBot.Core.View.BasicView>()
    .AddSingleton<TASagentTwitchBot.Core.ErrorHandler>()
    .AddSingleton<TASagentTwitchBot.Core.ApplicationManagement>()
    .AddSingleton<TASagentTwitchBot.Core.IMessageAccumulator, TASagentTwitchBot.Core.MessageAccumulator>();

//Custom Configurator
builder.Services
    .AddSingleton<TASagentTwitchBot.Core.IConfigurator, TASagentTwitchBot.BasicMicController.AudioConfigurator>();

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

//Midi System
builder.Services.RegisterMidiServices();

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

//Authentication Middleware
app.UseMiddleware<TASagentTwitchBot.Core.Web.Middleware.AuthCheckerMiddleware>();

//Map all Core Non-excluded controllers
app.MapControllers();

//Core Control Page Hub
app.MapHub<TASagentTwitchBot.Core.Web.Hubs.MonitorHub>("/Hubs/Monitor");

await app.StartAsync();


//
// Construct and run Configurator
//

TASagentTwitchBot.Core.ICommunication communication = app.Services.GetRequiredService<TASagentTwitchBot.Core.ICommunication>();
TASagentTwitchBot.Core.IConfigurator configurator = app.Services.GetRequiredService<TASagentTwitchBot.Core.IConfigurator>();

app.Services.GetRequiredService<TASagentTwitchBot.Core.View.IConsoleOutput>();

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

communication.SendDebugMessage("*** Starting Up Basic Mic Application ***");

TASagentTwitchBot.Core.ErrorHandler errorHandler = app.Services.GetRequiredService<TASagentTwitchBot.Core.ErrorHandler>();
TASagentTwitchBot.Core.ApplicationManagement applicationManagement = app.Services.GetRequiredService<TASagentTwitchBot.Core.ApplicationManagement>();

app.Services.GetRequiredService<TASagentTwitchBot.Core.Audio.IMicrophoneHandler>();
app.Services.GetRequiredService<TASagentTwitchBot.Core.IMessageAccumulator>();

app.Services.ConstructRequiredMidiServices();


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