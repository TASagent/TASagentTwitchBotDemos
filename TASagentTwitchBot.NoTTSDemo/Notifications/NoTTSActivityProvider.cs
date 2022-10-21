using System.Web;

using TASagentTwitchBot.Core;
using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.Audio.Effects;
using TASagentTwitchBot.Core.Notifications;
using TASagentTwitchBot.Core.Donations;

namespace TASagentTwitchBot.NoTTSDemo.Notifications;

public class NoTTSActivityProvider :
    IActivityHandler,
    ISubscriptionHandler,
    ICheerHandler,
    IRaidHandler,
    IGiftSubHandler,
    IFollowerHandler,
    IDisposable
{
    protected readonly ICommunication communication;
    protected readonly IActivityDispatcher activityDispatcher;
    protected readonly ISoundEffectSystem soundEffectSystem;
    protected readonly IAudioPlayer audioPlayer;
    protected readonly IAudioEffectSystem audioEffectSystem;
    protected readonly INotificationImageHelper notificationImageHelper;
    protected readonly NotificationServer notificationServer;
    protected readonly Core.Bits.CheerHelper cheerHelper;

    protected readonly Core.Database.IUserHelper userHelper;

    protected readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();

    private IDonationTracker? donationTracker = null;

    protected readonly HashSet<string> followedUserIds = new HashSet<string>();

    private bool disposedValue;

    public NoTTSActivityProvider(
        ICommunication communication,
        ISoundEffectSystem soundEffectSystem,
        IAudioPlayer audioPlayer,
        IAudioEffectSystem audioEffectSystem,
        Core.Bits.CheerHelper cheerHelper,
        INotificationImageHelper notificationImageHelper,
        IActivityDispatcher activityDispatcher,
        NotificationServer notificationServer,
        Core.Database.IUserHelper userHelper)
    {
        this.communication = communication;

        this.soundEffectSystem = soundEffectSystem;
        this.audioEffectSystem = audioEffectSystem;
        this.audioPlayer = audioPlayer;
        this.cheerHelper = cheerHelper;
        this.notificationImageHelper = notificationImageHelper;

        this.activityDispatcher = activityDispatcher;
        this.notificationServer = notificationServer;

        this.userHelper = userHelper;
    }

    #region IActivityHandler

    public Task Execute(ActivityRequest activityRequest)
    {
        List<Task> taskList = new List<Task>();

        if (activityRequest is IOverlayActivity overlayActivity && overlayActivity.NotificationMessage is not null)
        {
            taskList.Add(notificationServer.ShowNotificationAsync(overlayActivity.NotificationMessage));
        }

        if (activityRequest is IAudioActivity audioActivity && audioActivity.AudioRequest is not null)
        {
            taskList.Add(audioPlayer.PlayAudioRequest(audioActivity.AudioRequest));
        }

        return Task.WhenAll(taskList).WithCancellation(generalTokenSource.Token);
    }

    public void RegisterDonationTracker(IDonationTracker donationTracker)
    {
        if (this.donationTracker is not null)
        {
            throw new Exception($"donationTracker already assigned: Was \"{this.donationTracker}\", Assigned \"{donationTracker}\"");
        }

        this.donationTracker = donationTracker;
    }

    #endregion IActivityHandler
    #region ISubscriptionHandler

    public virtual async void HandleSubscription(
        string userId,
        string message,
        int monthCount,
        int tier,
        bool approved)
    {
        donationTracker?.AddSubs(1, tier);

        Core.Database.User? subscriber = await userHelper.GetUserByTwitchId(userId);

        if (subscriber is null)
        {
            communication.SendErrorMessage($"Unable to find user {userId} for Subscription handling");
            return;
        }

        communication.NotifyEvent($"Tier {tier} Sub: {subscriber.TwitchUserName}");

        string? chatResponse = await GetSubscriberChatResponse(subscriber, message, monthCount, tier);
        if (!string.IsNullOrWhiteSpace(chatResponse))
        {
            communication.SendPublicChatMessage(chatResponse);
        }

        activityDispatcher.QueueActivity(
            activity: new NoTTSActivityRequest(
                activityHandler: this,
                description: $"Sub: {subscriber.TwitchUserName}: {message ?? ""}",
                requesterId: subscriber.TwitchUserId,
                notificationMessage: await GetSubscriberNotificationRequest(subscriber, (message ?? ""), monthCount, tier),
                audioRequest: await GetSubscriberAudioRequest(subscriber, (message ?? ""), monthCount, tier)),
            approved: approved);
    }

    protected virtual Task<string> GetSubscriberChatResponse(
        Core.Database.User subscriber,
        string message,
        int monthCount,
        int tier)
    {
        if (monthCount <= 1)
        {
            return Task.FromResult($"Holy Cow! Thanks for the sub, {subscriber.TwitchUserName}!");
        }

        return Task.FromResult($"Holy Cow! Thanks for {monthCount} months, {subscriber.TwitchUserName}!");
    }

    protected virtual Task<Core.Notifications.NotificationMessage> GetSubscriberNotificationRequest(
        Core.Database.User subscriber,
        string message,
        int monthCount,
        int tier)
    {
        return Task.FromResult<Core.Notifications.NotificationMessage>(new ImageNotificationMessage(
            image: notificationImageHelper.GetRandomDefaultImageURL(),
            duration: 5000,
            message: GetSubscriberNotificationMessage(subscriber, message, monthCount, tier)));
    }

    protected virtual Task<AudioRequest?> GetSubscriberAudioRequest(
        Core.Database.User subscriber,
        string message,
        int monthCount,
        int tier)
    {
        AudioRequest? soundEffectRequest = null;

        if (soundEffectSystem.HasSoundEffects())
        {
            SoundEffect? subSoundEffect = soundEffectSystem.GetSoundEffectByName("SMW PowerUp");
            if (subSoundEffect is null)
            {
                communication.SendWarningMessage($"Expected Sub SoundEffect not found.  Defaulting to first sound effect.");
                subSoundEffect = soundEffectSystem.GetAnySoundEffect();
            }

            if (subSoundEffect is not null)
            {
                soundEffectRequest = new SoundEffectRequest(subSoundEffect);
            }
        }

        return Task.FromResult(AudioTools.JoinRequests(300, soundEffectRequest));
    }

    protected virtual string GetSubscriberNotificationMessage(
        Core.Database.User subscriber,
        string message,
        int monthCount,
        int tier)
    {
        string? fontColor = subscriber.Color;
        if (string.IsNullOrWhiteSpace(fontColor))
        {
            fontColor = "#0000FF";
        }

        if (monthCount == 1)
        {
            switch (tier)
            {
                case 0: return $"Thank you for the brand new Prime Gaming sub, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber)}</span>!";
                case 1: return $"Thank you for the brand new sub, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber)}</span>!";
                case 2: return $"Thank you for the brand new tier 2 sub, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber)}</span>!";
                case 3: return $"Thank you for the brand new tier 3 sub, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber)}</span>!";
                default:
                    BGC.Debug.LogError($"Unexpected SubscriberNotification {tier} tier, {monthCount} months.");
                    return $"Thank you for the brand new sub, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber)}</span>!";
            }
        }
        else
        {
            switch (tier)
            {
                case 0: return $"Thank you for subscribing for {monthCount} months with Prime Gaming, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber)}</span>!";
                case 1: return $"Thank you for subscribing for {monthCount} months, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber)}</span>!";
                case 2: return $"Thank you for subscribing at tier 2 for {monthCount} months, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber)}</span>!";
                case 3: return $"Thank you for subscribing at tier 3 for {monthCount} months, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber)}</span>!";
                default:
                    BGC.Debug.LogError($"Unexpected SubscriberNotification {tier} tier, {monthCount} months.");
                    return $"Thank you for subscribing for {monthCount} months, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(subscriber)}</span>!";
            }
        }
    }

    #endregion ISubscriptionHandler
    #region ICheerHandler

    public virtual async void HandleCheer(
        Core.Database.User cheerer,
        string message,
        int quantity,
        bool meetsTTSThreshold,
        bool approved)
    {
        donationTracker?.AddBits(quantity);

        communication.NotifyEvent($"Cheer {quantity}: {cheerer.TwitchUserName}");

        string? chatResponse = await GetCheerChatResponse(cheerer, message, quantity);
        if (!string.IsNullOrWhiteSpace(chatResponse))
        {
            communication.SendPublicChatMessage(chatResponse);
        }

        activityDispatcher.QueueActivity(
            activity: new NoTTSActivityRequest(
                activityHandler: this,
                description: $"User {cheerer.TwitchUserName} cheered {quantity} bits: {message}",
                requesterId: cheerer.TwitchUserId,
                notificationMessage: await GetCheerNotificationRequest(cheerer, message, quantity),
                audioRequest: await GetCheerAudioRequest(cheerer, message, quantity)),
            approved: approved);
    }

    protected virtual Task<string?> GetCheerChatResponse(
        Core.Database.User cheerer,
        string message,
        int quantity)
    {
        return Task.FromResult<string?>(null);
    }

    protected virtual async Task<Core.Notifications.NotificationMessage> GetCheerNotificationRequest(
        Core.Database.User cheerer,
        string message,
        int quantity)
    {
        return new ImageNotificationMessage(
            image: await cheerHelper.GetCheerImageURL(message, quantity),
            duration: 10_000,
            message: GetCheerMessage(cheerer, message, quantity));
    }

    protected virtual string GetCheerMessage(
        Core.Database.User cheerer,
        string message,
        int quantity)
    {
        string? fontColor = cheerer.Color;
        if (string.IsNullOrWhiteSpace(fontColor))
        {
            fontColor = "#0000FF";
        }

        return $"<span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(cheerer.TwitchUserName)}</span> has cheered {quantity} {(quantity == 1 ? "bit" : "bits")}: {HttpUtility.HtmlEncode(message)}";
    }

    protected virtual Task<AudioRequest?> GetCheerAudioRequest(
        Core.Database.User cheerer,
        string message,
        int quantity)
    {
        AudioRequest? soundEffectRequest = null;

        if (soundEffectSystem.HasSoundEffects())
        {
            SoundEffect? cheerSoundEffect = soundEffectSystem.GetSoundEffectByName("FF7 Purchase");
            if (cheerSoundEffect is null)
            {
                communication.SendWarningMessage($"Expected Cheer SoundEffect not found.  Defaulting to first");
                cheerSoundEffect = soundEffectSystem.GetAnySoundEffect();
            }

            if (cheerSoundEffect is not null)
            {
                soundEffectRequest = new SoundEffectRequest(cheerSoundEffect);
            }
        }

        return Task.FromResult(AudioTools.JoinRequests(300, soundEffectRequest));
    }

    #endregion ICheerHandler
    #region IRaidHandler

    public virtual async void HandleRaid(
        string raiderId,
        int count,
        bool approved)
    {
        Core.Database.User? raider = await userHelper.GetUserByTwitchId(raiderId);

        if (raider is null)
        {
            communication.SendErrorMessage($"Unable to find user {raiderId} for Raid handling");
            return;
        }

        communication.NotifyEvent($"{count} Raid: {raider.TwitchUserName}");

        string chatResponse = await GetRaidChatResponse(raider, count);
        if (!string.IsNullOrWhiteSpace(chatResponse))
        {
            communication.SendPublicChatMessage(chatResponse);
        }

        activityDispatcher.QueueActivity(
            activity: new NoTTSActivityRequest(
                activityHandler: this,
                description: $"Raid: {raider} with {count} viewers",
                requesterId: raider.TwitchUserId,
                notificationMessage: await GetRaidNotificationRequest(raider, count),
                audioRequest: await GetRaidAudioRequest(raider, count)),
            approved: approved);
    }

    protected virtual Task<string> GetRaidChatResponse(
        Core.Database.User raider,
        int count)
    {
        return Task.FromResult($"Wow! {raider.TwitchUserName} has Raided with {count} viewers! PogChamp");
    }

    protected virtual Task<Core.Notifications.NotificationMessage> GetRaidNotificationRequest(
        Core.Database.User raider,
        int count)
    {
        return Task.FromResult<Core.Notifications.NotificationMessage>(new ImageNotificationMessage(
            image: notificationImageHelper.GetRandomDefaultImageURL(),
            duration: 10_000,
            message: $"WOW! {count} raiders incoming from {HttpUtility.HtmlEncode(raider.TwitchUserName)}!"));
    }

    protected virtual Task<AudioRequest?> GetRaidAudioRequest(
        Core.Database.User raider,
        int count)
    {
        AudioRequest? soundEffectRequest = null;

        if (soundEffectSystem.HasSoundEffects())
        {
            SoundEffect? raidSoundEffect = soundEffectSystem.GetSoundEffectByName("SMW CastleClear");
            if (raidSoundEffect is null)
            {
                communication.SendWarningMessage($"Expected Raid SoundEffect not found.  Defaulting to first");
                raidSoundEffect = soundEffectSystem.GetAnySoundEffect();
            }

            if (raidSoundEffect is not null)
            {
                soundEffectRequest = new SoundEffectRequest(raidSoundEffect);
            }
        }

        return Task.FromResult(soundEffectRequest);
    }

    #endregion IRaidHandler
    #region IGiftSubHandler

    public virtual async void HandleGiftSub(
        string senderId,
        string recipientId,
        int tier,
        int months,
        bool approved)
    {
        donationTracker?.AddSubs(1, tier);

        Core.Database.User? sender = await userHelper.GetUserByTwitchId(senderId);
        Core.Database.User? recipient = await userHelper.GetUserByTwitchId(recipientId);

        if (sender is null)
        {
            communication.SendErrorMessage($"Unable to find sender {senderId} for Gift Sub handling");
            return;
        }

        if (recipient is null)
        {
            communication.SendErrorMessage($"Unable to find reciever {recipientId} for Gift Sub handling");
            return;
        }

        communication.NotifyEvent($"Gift Sub from {sender.TwitchUserName} to {recipient.TwitchUserName}");

        string? chatResponse = await GetGiftSubChatResponse(sender, recipient, tier, months);
        if (!string.IsNullOrWhiteSpace(chatResponse))
        {
            communication.SendPublicChatMessage(chatResponse);
        }

        activityDispatcher.QueueActivity(
            activity: new NoTTSActivityRequest(
                activityHandler: this,
                description: $"Gift Sub To: {recipientId}",
                requesterId: sender.TwitchUserId,
                notificationMessage: await GetGiftSubNotificationRequest(sender, recipient, tier, months),
                audioRequest: await GetGiftSubAudioRequest(sender, recipient, tier, months)),
            approved: approved);
    }

    protected virtual Task<string?> GetGiftSubChatResponse(
        Core.Database.User sender,
        Core.Database.User recipient,
        int tier,
        int months)
    {
        return Task.FromResult<string?>(null);
    }

    protected virtual Task<Core.Notifications.NotificationMessage> GetGiftSubNotificationRequest(
        Core.Database.User sender,
        Core.Database.User recipient,
        int tier,
        int months)
    {
        return Task.FromResult<Core.Notifications.NotificationMessage>(new ImageNotificationMessage(
            image: notificationImageHelper.GetRandomDefaultImageURL(),
            duration: 5_000,
            message: GetGiftSubNotificationMessage(sender, recipient, tier, months)));
    }

    protected virtual Task<AudioRequest?> GetGiftSubAudioRequest(
        Core.Database.User sender,
        Core.Database.User recipient,
        int tier,
        int months)
    {
        AudioRequest? soundEffectRequest = null;

        if (soundEffectSystem.HasSoundEffects())
        {
            SoundEffect? giftSubSoundEffect = soundEffectSystem.GetSoundEffectByName("SMW PowerUp");
            if (giftSubSoundEffect is null)
            {
                communication.SendWarningMessage($"Expected GiftSub SoundEffect not found.  Defaulting to first");
                giftSubSoundEffect = soundEffectSystem.GetAnySoundEffect();
            }

            if (giftSubSoundEffect is not null)
            {
                soundEffectRequest = new SoundEffectRequest(giftSubSoundEffect);
            }
        }

        return Task.FromResult(soundEffectRequest);
    }

    protected virtual string GetGiftSubNotificationMessage(
        Core.Database.User sender,
        Core.Database.User recipient,
        int tier,
        int months)
    {
        if (months <= 1)
        {
            switch (tier)
            {
                case 0: return $"It's possible to give a sub with Prime Gaming? Who Knew? Thank you, {HttpUtility.HtmlEncode(sender.TwitchUserName)}, for gifting a sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                case 1: return $"Thank you, {HttpUtility.HtmlEncode(sender.TwitchUserName)}, for gifting a sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                case 2: return $"Thank you, {HttpUtility.HtmlEncode(sender.TwitchUserName)}, for gifting a tier 2 sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                case 3: return $"Thank you, {HttpUtility.HtmlEncode(sender.TwitchUserName)}, for gifting a tier 3 sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                default:
                    BGC.Debug.LogError($"Unexpected SubscriberNotification Values: {sender.TwitchUserName} sender, {recipient.TwitchUserName} recipient, {tier} tier");
                    return $"Thank you, {HttpUtility.HtmlEncode(sender.TwitchUserName)}, for gifting a sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
            }
        }
        else
        {
            switch (tier)
            {
                case 0: return $"It's possible to give a sub with Prime Gaming? Who Knew? Thank you, {HttpUtility.HtmlEncode(sender.TwitchUserName)}, for gifting a sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                case 1: return $"Thank you, {HttpUtility.HtmlEncode(sender.TwitchUserName)}, for gifting {months} months to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                case 2: return $"Thank you, {HttpUtility.HtmlEncode(sender.TwitchUserName)}, for gifting {months} months of tier 2 to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                case 3: return $"Thank you, {HttpUtility.HtmlEncode(sender.TwitchUserName)}, for gifting {months} months of tier 3 to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                default:
                    BGC.Debug.LogError($"Unexpected SubscriberNotification Values: {sender.TwitchUserName} sender, {recipient.TwitchUserName} recipient, {tier} tier");
                    return $"Thank you, {HttpUtility.HtmlEncode(sender.TwitchUserName)}, for gifting a sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
            }
        }
    }

    public virtual async void HandleAnonGiftSub(
        string recipientId,
        int tier,
        int months,
        bool approved)
    {
        donationTracker?.AddSubs(1, tier);

        Core.Database.User? recipient = await userHelper.GetUserByTwitchId(recipientId);

        if (recipient is null)
        {
            communication.SendErrorMessage($"Unable to find reciever {recipientId} for Gift Sub handling");
            return;
        }

        communication.NotifyEvent($"Gift Sub from Anon to {recipient}");

        string? chatResponse = await GetAnonGiftSubChatResponse(recipient, tier, months);
        if (!string.IsNullOrWhiteSpace(chatResponse))
        {
            communication.SendPublicChatMessage(chatResponse);
        }

        activityDispatcher.QueueActivity(
            activity: new NoTTSActivityRequest(
                activityHandler: this,
                description: $"Anon Gift Sub To: {recipient}",
                requesterId: "",
                notificationMessage: await GetAnonGiftSubNotificationRequest(recipient, tier, months),
                audioRequest: await GetAnonGiftSubAudioRequest(recipient, tier, months)),
            approved: approved);
    }

    protected virtual Task<string?> GetAnonGiftSubChatResponse(
        Core.Database.User recipient,
        int tier,
        int months)
    {
        return Task.FromResult<string?>(null);
    }

    protected virtual Task<Core.Notifications.NotificationMessage> GetAnonGiftSubNotificationRequest(
        Core.Database.User recipient,
        int tier,
        int months)
    {
        return Task.FromResult<Core.Notifications.NotificationMessage>(new ImageNotificationMessage(
            image: notificationImageHelper.GetRandomDefaultImageURL(),
            duration: 5_000,
            message: GetAnonGiftSubNotificationMessage(recipient, tier, months)));
    }

    protected virtual Task<AudioRequest?> GetAnonGiftSubAudioRequest(
        Core.Database.User recipient,
        int tier,
        int months)
    {
        AudioRequest? soundEffectRequest = null;

        if (soundEffectSystem.HasSoundEffects())
        {
            SoundEffect? anonGiftSubSoundEffect = soundEffectSystem.GetSoundEffectByName("SMW PowerUp");
            if (anonGiftSubSoundEffect is null)
            {
                communication.SendWarningMessage($"Expected GiftSub SoundEffect not found.  Defaulting to first");
                anonGiftSubSoundEffect = soundEffectSystem.GetAnySoundEffect();
            }

            if (anonGiftSubSoundEffect is not null)
            {
                soundEffectRequest = new SoundEffectRequest(anonGiftSubSoundEffect);
            }
        }

        return Task.FromResult(soundEffectRequest);
    }

    protected virtual string GetAnonGiftSubNotificationMessage(
        Core.Database.User recipient,
        int tier,
        int months)
    {
        if (months <= 1)
        {
            switch (tier)
            {
                case 0: return $"It's possible to give a sub with Prime Gaming? Who Knew? Thank you, Anonymous, for gifting a sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                case 1: return $"Thank you, Anonymous, for gifting a sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                case 2: return $"Thank you, Anonymous, for gifting a tier 2 sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                case 3: return $"Thank you, Anonymous, for gifting a tier 3 sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                default:
                    BGC.Debug.LogError($"Unexpected SubscriberNotification Values: {recipient.TwitchUserName} recipient, {tier} tier, {months} months");
                    return $"Thank you, Anonymous, for gifting a sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
            }
        }
        else
        {
            switch (tier)
            {
                case 0: return $"It's possible to give a sub with Prime Gaming? Who Knew? Thank you, Anonymous, for gifting a sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                case 1: return $"Thank you, Anonymous, for gifting {months} months to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                case 2: return $"Thank you, Anonymous, for gifting {months} months of tier 2 to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                case 3: return $"Thank you, Anonymous, for gifting {months} months of tier 3 to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
                default:
                    BGC.Debug.LogError($"Unexpected SubscriberNotification Values: {recipient.TwitchUserName} recipient, {tier} tier, {months} months");
                    return $"Thank you, Anonymous, for gifting a sub to {HttpUtility.HtmlEncode(recipient.TwitchUserName)}!";
            }
        }
    }

    #endregion IGiftSubHandler
    #region IFollowerHandler

    public virtual async void HandleFollower(
        Core.Database.User follower,
        bool approved)
    {
        if (followedUserIds.Add(follower.TwitchUserId))
        {
            communication.NotifyEvent($"Follow: {follower.TwitchUserName}");
        }
        else
        {
            communication.NotifyEvent($"Re-Follow: {follower.TwitchUserName}");
            //Skip notifications
            return;
        }

        string? chatResponse = await GetFollowChatResponse(follower);
        if (!string.IsNullOrWhiteSpace(chatResponse))
        {
            communication.SendPublicChatMessage(chatResponse);
        }

        activityDispatcher.QueueActivity(
            activity: new NoTTSActivityRequest(
                activityHandler: this,
                description: $"Follower: {follower.TwitchUserName}",
                requesterId: follower.TwitchUserId,
                notificationMessage: await GetFollowNotificationRequest(follower),
                audioRequest: await GetFollowAudioRequest(follower)),
            approved: approved);
    }

    protected virtual Task<string> GetFollowChatResponse(Core.Database.User follower)
    {
        return Task.FromResult($"Thanks for following, @{follower.TwitchUserName}");
    }

    protected virtual Task<Core.Notifications.NotificationMessage> GetFollowNotificationRequest(Core.Database.User follower)
    {
        return Task.FromResult<Core.Notifications.NotificationMessage>(new ImageNotificationMessage(
            image: notificationImageHelper.GetRandomDefaultImageURL(),
            duration: 4_000,
            message: GetFollowNotificationMessage(follower)));
    }

    protected virtual Task<AudioRequest?> GetFollowAudioRequest(Core.Database.User follower)
    {
        AudioRequest? soundEffectRequest = null;

        if (soundEffectSystem.HasSoundEffects())
        {
            SoundEffect? followSoundEffect = soundEffectSystem.GetSoundEffectByName("SMW MessageBlock");
            if (followSoundEffect is null)
            {
                communication.SendWarningMessage($"Expected Follow SoundEffect not found.  Defaulting to first");
                followSoundEffect = soundEffectSystem.GetAnySoundEffect();
            }

            if (followSoundEffect is not null)
            {
                soundEffectRequest = new SoundEffectRequest(followSoundEffect);
            }
        }

        return Task.FromResult(soundEffectRequest);
    }

    protected virtual string GetFollowNotificationMessage(Core.Database.User follower)
    {
        string? fontColor = follower.Color;
        if (string.IsNullOrWhiteSpace(fontColor))
        {
            fontColor = "#0000FF";
        }

        return $"Thanks for the following, <span style=\"color: {fontColor}\">{HttpUtility.HtmlEncode(follower.TwitchUserName)}</span>!";
    }

    #endregion IFollowerHandler

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

    public class NoTTSActivityRequest : ActivityRequest, IOverlayActivity, IAudioActivity
    {
        public Core.Notifications.NotificationMessage? NotificationMessage { get; }
        public AudioRequest? AudioRequest { get; }

        public NoTTSActivityRequest(
            IActivityHandler activityHandler,
            string description,
            string requesterId,
            Core.Notifications.NotificationMessage? notificationMessage = null,
            AudioRequest? audioRequest = null)
            : base(activityHandler, description, requesterId)
        {
            NotificationMessage = notificationMessage;
            AudioRequest = audioRequest;
        }
    }
}
