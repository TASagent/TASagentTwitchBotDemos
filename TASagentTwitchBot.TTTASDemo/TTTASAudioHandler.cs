using TASagentTwitchBot.Core;

namespace TASagentTwitchBot.TTTASDemo;

public class TTTASAudioHandler : Plugin.TTTAS.ITTTASHandler, IDisposable
{
    private readonly ICommunication communication;
    private readonly Core.Notifications.IActivityDispatcher activityDispatcher;
    private readonly Core.Audio.ISoundEffectSystem soundEffectSystem;
    private readonly Plugin.TTTAS.ITTTASRenderer tttasRenderer;
    private readonly Core.Audio.IAudioPlayer audioPlayer;

    private readonly Plugin.TTTAS.TTTASConfiguration tttasConfig;

    private readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();

    private bool disposedValue;

    public TTTASAudioHandler(
        ICommunication communication,
        Core.Notifications.IActivityDispatcher activityDispatcher,
        Core.Audio.ISoundEffectSystem soundEffectSystem,
        Core.Audio.IAudioPlayer audioPlayer,
        Plugin.TTTAS.ITTTASRenderer tttasRenderer,
        Plugin.TTTAS.TTTASConfiguration tttasConfig)
    {
        this.communication = communication;
        this.activityDispatcher = activityDispatcher;
        this.soundEffectSystem = soundEffectSystem;
        this.audioPlayer = audioPlayer;
        this.tttasRenderer = tttasRenderer;

        this.tttasConfig = tttasConfig;
    }

    public async void HandleTTTAS(
        Core.Database.User user,
        string message,
        bool approved)
    {
        activityDispatcher.QueueActivity(
            activity: new TTTASActivityRequest(
                tttasAudioHandler: this,
                description: $"{tttasConfig.FeatureNameBrief} {user.TwitchUserName}: {message}",
                audioRequest: await GetTTTASAudioRequest(user, message)),
            approved: approved);
    }

    private async Task<Core.Audio.AudioRequest?> GetTTTASAudioRequest(Core.Database.User _, string message)
    {
        Core.Audio.AudioRequest? soundEffectRequest = null;
        Core.Audio.AudioRequest? tttasRequest = null;

        if (!string.IsNullOrEmpty(tttasConfig.SoundEffect) && soundEffectSystem.HasSoundEffects())
        {
            Core.Audio.SoundEffect? tttasSoundEffect = soundEffectSystem.GetSoundEffectByName(tttasConfig.SoundEffect);
            if (tttasSoundEffect is null)
            {
                communication.SendWarningMessage($"Expected {tttasConfig.FeatureNameBrief} SoundEffect \"{tttasConfig.SoundEffect}\" not found.  Defaulting to first sound effect.");
                tttasSoundEffect = soundEffectSystem.GetSoundEffectByName(soundEffectSystem.GetSoundEffects()[0]);
            }

            if (tttasSoundEffect is not null)
            {
                soundEffectRequest = new Core.Audio.SoundEffectRequest(tttasSoundEffect);
            }
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            tttasRequest = await tttasRenderer.TTTASRequest(
                tttasText: message);
        }

        return Core.Notifications.FullActivityProvider.JoinRequests(300, soundEffectRequest, tttasRequest);
    }

    private async Task Execute(TTTASActivityRequest activityRequest)
    {
        if (activityRequest.AudioRequest is not null)
        {
            await audioPlayer.PlayAudioRequest(activityRequest.AudioRequest).WithCancellation(generalTokenSource.Token);
        }
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

    public class TTTASActivityRequest : Core.Notifications.ActivityRequest
    {
        private readonly TTTASAudioHandler tttasAudioHandler;
        public Core.Audio.AudioRequest? AudioRequest { get; }

        private readonly string description;

        public TTTASActivityRequest(
            TTTASAudioHandler tttasAudioHandler,
            string description,
            Core.Audio.AudioRequest? audioRequest = null)
        {
            this.tttasAudioHandler = tttasAudioHandler;
            this.description = description;

            AudioRequest = audioRequest;
        }

        public override Task Execute() => tttasAudioHandler.Execute(this);
        public override string ToString() => description;
    }
}
