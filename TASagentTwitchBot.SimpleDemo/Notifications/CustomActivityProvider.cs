using TASagentTwitchBot.Core.Notifications;

namespace TASagentTwitchBot.SimpleDemo.Notifications;

public class CustomActivityProvider : FullActivityProvider
{
    public CustomActivityProvider(
        Core.ICommunication communication,
        Core.Audio.ISoundEffectSystem soundEffectSystem,
        Core.Audio.IAudioPlayer audioPlayer,
        Core.Audio.Effects.IAudioEffectSystem audioEffectSystem,
        Core.Bits.CheerHelper cheerHelper,
        IActivityDispatcher activityDispatcher,
        Core.TTS.ITTSRenderer ttsRenderer,
        NotificationServer notificationServer,
        Core.Database.IUserHelper userHelper)
        : base(
              communication: communication,
              soundEffectSystem: soundEffectSystem,
              audioPlayer: audioPlayer,
              audioEffectSystem: audioEffectSystem,
              cheerHelper: cheerHelper,
              activityDispatcher: activityDispatcher,
              ttsRenderer: ttsRenderer,
              notificationServer: notificationServer,
              userHelper: userHelper)
    {

    }

    protected override Task<string> GetFollowChatResponse(Core.Database.User follower)
    {
        return Task.FromResult("Thanks for following!");
    }

    protected override string GetFollowNotificationMessage(Core.Database.User follower)
    {
        return "Thanks for following!";
    }

    public void TestNotification()
    {
        Core.Audio.SoundEffect? testSoundEffect = soundEffectSystem.GetSoundEffectByName("SMW MessageBlock");
        Core.Audio.SoundEffectRequest? soundEffectRequest = null;

        if (testSoundEffect is null)
        {
            communication.SendWarningMessage($"Expected Test SoundEffect not found.  Defaulting to first");
            testSoundEffect = soundEffectSystem.GetAnySoundEffect();
        }

        if (testSoundEffect is not null)
        {
            soundEffectRequest = new Core.Audio.SoundEffectRequest(testSoundEffect);
        }

        activityDispatcher.QueueActivity(
            activity: new FullActivityRequest(
                fullActivityProvider: this,
                description: "Test",
                notificationMessage: new ImageNotificationMessage(notificationServer.GetNextImageURL(), 4000, "Test!"),
                audioRequest: soundEffectRequest),
            approved: true);
    }
}
