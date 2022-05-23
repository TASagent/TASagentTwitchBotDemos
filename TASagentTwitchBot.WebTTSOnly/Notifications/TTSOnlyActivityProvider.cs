using Microsoft.AspNetCore.SignalR;

using TASagentTwitchBot.Core;
using TASagentTwitchBot.Core.Database;
using TASagentTwitchBot.Core.Notifications;
using TASagentTwitchBot.Core.Web.Hubs;

namespace TASagentTwitchBot.WebTTSOnly.Notifications;

public class TTSOnlyActivityProvider :
    IActivityHandler,
    ITTSHandler,
    IDisposable
{
    private readonly Core.Audio.IAudioPlayer audioPlayer;
    private readonly Core.Audio.Effects.IAudioEffectSystem audioEffectSystem;
    private readonly IActivityDispatcher activityDispatcher;
    private readonly Core.TTS.ITTSRenderer ttsRenderer;
    private readonly IHubContext<TTSMarqueeHub> ttsMarqueeHubContext;

    private readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();

    private bool disposedValue = false;

    public TTSOnlyActivityProvider(
        Core.Audio.IAudioPlayer audioPlayer,
        Core.Audio.Effects.IAudioEffectSystem audioEffectSystem,
        IActivityDispatcher activityDispatcher,
        Core.TTS.ITTSRenderer ttsRenderer,
        IHubContext<TTSMarqueeHub> ttsMarqueeHubContext)
    {
        this.audioPlayer = audioPlayer;
        this.audioEffectSystem = audioEffectSystem;
        this.activityDispatcher = activityDispatcher;
        this.ttsRenderer = ttsRenderer;
        this.ttsMarqueeHubContext = ttsMarqueeHubContext;
    }

    public Task Execute(ActivityRequest activityRequest)
    {
        List<Task> taskList = new List<Task>();

        if (activityRequest is IAudioActivity audioActivity && audioActivity.AudioRequest is not null)
        {
            taskList.Add(audioPlayer.PlayAudioRequest(audioActivity.AudioRequest));
        }

        if (activityRequest is IMarqueeMessageActivity marqueeMessageActivity && marqueeMessageActivity.MarqueeMessage is not null)
        {
            //Don't bother waiting on this one to complete
            taskList.Add(ttsMarqueeHubContext.Clients.All.SendAsync("ReceiveTTSNotification",
                marqueeMessageActivity.MarqueeMessage.GetMessage()));
        }

        return Task.WhenAll(taskList).WithCancellation(generalTokenSource.Token);
    }

    #region ITTSHandler

    Task<bool> ITTSHandler.SetTTSEnabled(bool enabled) => ttsRenderer.SetTTSEnabled(enabled);

    public virtual async void HandleTTS(
        User user,
        string message,
        bool approved)
    {
        activityDispatcher.QueueActivity(
            activity: new TTSActivityRequest(
                activityHandler: this,
                description: $"TTS {user.TwitchUserName} : {message}",
                audioRequest: await GetTTSAudioRequest(user, message),
                marqueeMessage: new MarqueeMessage(user.TwitchUserName, message, user.Color)),
            approved: approved);
    }

    private Task<Core.Audio.AudioRequest?> GetTTSAudioRequest(
        User user,
        string message)
    {
        return ttsRenderer.TTSRequest(
            authorizationLevel: user.AuthorizationLevel,
            voicePreference: user.TTSVoicePreference,
            pitchPreference: user.TTSPitchPreference,
            speedPreference: user.TTSSpeedPreference,
            effectsChain: audioEffectSystem.SafeParse(user.TTSEffectsChain),
            ttsText: message);
    }

    #endregion ITTSHandler

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                generalTokenSource.Cancel();
                generalTokenSource.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public class TTSActivityRequest : ActivityRequest, IAudioActivity, IMarqueeMessageActivity
    {
        public Core.Audio.AudioRequest? AudioRequest { get; }
        public MarqueeMessage? MarqueeMessage { get; }

        public TTSActivityRequest(
            IActivityHandler activityHandler,
            string description,
            Core.Audio.AudioRequest? audioRequest = null,
            MarqueeMessage? marqueeMessage = null)
            : base(activityHandler, description)
        {
            AudioRequest = audioRequest;
            MarqueeMessage = marqueeMessage;
        }
    }
}
