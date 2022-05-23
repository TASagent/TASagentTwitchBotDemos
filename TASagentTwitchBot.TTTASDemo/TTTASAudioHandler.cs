using TASagentTwitchBot.Core;
using TASagentTwitchBot.Core.Notifications;

namespace TASagentTwitchBot.TTTASDemo;

public class TTTASAudioHandler : 
    IActivityHandler,
    IDisposable
{
    private readonly Core.Audio.IAudioPlayer audioPlayer;

    private readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();

    private bool disposedValue;

    public TTTASAudioHandler(
        Core.Audio.IAudioPlayer audioPlayer)
    {
        this.audioPlayer = audioPlayer;
    }

    public async Task Execute(ActivityRequest activityRequest)
    {
        if (activityRequest is IAudioActivity audioActivity && audioActivity.AudioRequest is not null)
        {
            await audioPlayer.PlayAudioRequest(audioActivity.AudioRequest).WithCancellation(generalTokenSource.Token);
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
}
