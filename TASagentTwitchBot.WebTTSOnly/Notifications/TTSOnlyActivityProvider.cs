using Microsoft.AspNetCore.SignalR;

using TASagentTwitchBot.Core;
using TASagentTwitchBot.Core.Database;
using TASagentTwitchBot.Core.Notifications;
using TASagentTwitchBot.Core.Web.Hubs;

namespace TASagentTwitchBot.WebTTSOnly.Notifications;

public class TTSOnlyActivityProvider : 
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

    private Task Execute(TTSActivityRequest activityRequest)
    {
        List<Task> taskList = new List<Task>();

        if (activityRequest.AudioRequest is not null)
        {
            taskList.Add(audioPlayer.PlayAudioRequest(activityRequest.AudioRequest));
        }

        if (activityRequest.MarqueeMessage is not null)
        {
            //Don't bother waiting on this one to complete
            taskList.Add(ttsMarqueeHubContext.Clients.All.SendAsync("ReceiveTTSNotification",
                activityRequest.MarqueeMessage.GetMessage()));
        }

        return Task.WhenAll(taskList).WithCancellation(generalTokenSource.Token);
    }

    #region ITTSHandler

    public virtual async void HandleTTS(
        User user,
        string message,
        bool approved)
    {
        activityDispatcher.QueueActivity(
            activity: new TTSActivityRequest(
                activityProvider: this,
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

    public static Core.Audio.AudioRequest? JoinRequests(int delayMS, params Core.Audio.AudioRequest?[] audioRequests)
    {
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
        List<Core.Audio.AudioRequest> audioRequestList = new List<Core.Audio.AudioRequest>(audioRequests?.Where(x => x is not null) ?? Array.Empty<Core.Audio.AudioRequest?>());
#pragma warning restore CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.

        if (audioRequestList.Count == 0)
        {
            return null;
        }

        if (audioRequestList.Count == 1)
        {
            return audioRequestList[0];
        }

        for (int i = audioRequestList.Count - 1; i > 0; i--)
        {
            audioRequestList.Insert(i, new Core.Audio.AudioDelay(delayMS));
        }

        return new Core.Audio.ConcatenatedAudioRequest(audioRequestList);
    }

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

    public class TTSActivityRequest : ActivityRequest
    {
        private readonly TTSOnlyActivityProvider activityProvider;
        public Core.Audio.AudioRequest? AudioRequest { get; }
        public MarqueeMessage? MarqueeMessage { get; }

        private readonly string description;

        public TTSActivityRequest(
            TTSOnlyActivityProvider activityProvider,
            string description,
            Core.Audio.AudioRequest? audioRequest = null,
            MarqueeMessage? marqueeMessage = null)
        {
            this.activityProvider = activityProvider;
            this.description = description;

            AudioRequest = audioRequest;
            MarqueeMessage = marqueeMessage;
        }

        public override Task Execute() => activityProvider.Execute(this);
        public override string ToString() => description;
    }
}

public class ActivityProviderStubs : IRaidHandler, IGiftSubHandler, IFollowerHandler, ICheerHandler, ISubscriptionHandler
{
    public void HandleAnonGiftSub(string recipientId, int tier, int months, bool approved)
    {
        //Do nothing
    }

    public void HandleCheer(User cheerer, string message, int quantity, bool approved)
    {
        //Do nothing
    }

    public void HandleFollower(User follower, bool approved)
    {
        //Do nothing
    }

    public void HandleGiftSub(string senderId, string recipientId, int tier, int months, bool approved)
    {
        //Do nothing
    }

    public void HandleRaid(string raiderId, int count, bool approved)
    {
        //Do nothing
    }

    public void HandleSubscription(string userId, string message, int monthCount, int tier, bool approved)
    {
        //Do nothing
    }
}
