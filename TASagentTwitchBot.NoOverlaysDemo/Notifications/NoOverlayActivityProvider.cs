﻿using System.Web;

using TASagentTwitchBot.Core;
using TASagentTwitchBot.Core.Audio;
using TASagentTwitchBot.Core.Audio.Effects;
using TASagentTwitchBot.Core.Donations;
using TASagentTwitchBot.Core.Notifications;
using TASagentTwitchBot.Core.TTS;

namespace TASagentTwitchBot.NoOverlaysDemo.Notifications;

public class NoOverlayActivityProvider :
    IActivityHandler,
    ISubscriptionHandler,
    ICheerHandler,
    IRaidHandler,
    IGiftSubHandler,
    IFollowerHandler,
    ITTSHandler,
    IDisposable
{
    protected readonly ICommunication communication;
    protected readonly IActivityDispatcher activityDispatcher;
    protected readonly ISoundEffectSystem soundEffectSystem;
    protected readonly IAudioPlayer audioPlayer;
    protected readonly IAudioEffectSystem audioEffectSystem;
    protected readonly ITTSRenderer ttsRenderer;

    protected readonly Core.Database.IUserHelper userHelper;

    protected IDonationTracker? donationTracker = null;

    protected readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();

    protected readonly HashSet<string> followedUserIds = new HashSet<string>();

    private bool disposedValue;

    public NoOverlayActivityProvider(
        ICommunication communication,
        ISoundEffectSystem soundEffectSystem,
        IAudioPlayer audioPlayer,
        IAudioEffectSystem audioEffectSystem,
        IActivityDispatcher activityDispatcher,
        ITTSRenderer ttsRenderer,
        Core.Database.IUserHelper userHelper)
    {
        this.communication = communication;

        this.soundEffectSystem = soundEffectSystem;
        this.audioEffectSystem = audioEffectSystem;
        this.audioPlayer = audioPlayer;

        this.activityDispatcher = activityDispatcher;
        this.ttsRenderer = ttsRenderer;

        this.userHelper = userHelper;
    }

    #region IActivityHandler

    public Task Execute(ActivityRequest activityRequest)
    {
        List<Task> taskList = new List<Task>();

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
            activity: new AudioOnlyActivityRequest(
                activityHandler: this,
                description: $"Sub: {subscriber.TwitchUserName}: {message ?? ""}",
                requesterId: subscriber.TwitchUserId,
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

    protected virtual async Task<AudioRequest?> GetSubscriberAudioRequest(
        Core.Database.User subscriber,
        string message,
        int monthCount,
        int tier)
    {
        AudioRequest? soundEffectRequest = null;
        AudioRequest? ttsRequest = null;

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

        if (!string.IsNullOrWhiteSpace(message))
        {
            ttsRequest = await ttsRenderer.TTSRequest(
                authorizationLevel: Core.Commands.AuthorizationLevel.Admin,
                voicePreference: subscriber.TTSVoicePreference,
                pitchPreference: subscriber.TTSPitchPreference,
                speedPreference: subscriber.TTSSpeedPreference,
                effectsChain: audioEffectSystem.SafeParse(subscriber.TTSEffectsChain),
                ttsText: message);
        }

        return AudioTools.JoinRequests(300, soundEffectRequest, ttsRequest);
    }

    protected virtual string? GetSubscriberNotificationMessage(
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
            activity: new AudioOnlyActivityRequest(
                activityHandler: this,
                description: $"User {cheerer.TwitchUserName} cheered {quantity} bits: {message}",
                requesterId: cheerer.TwitchUserId,
                audioRequest: await GetCheerAudioRequest(cheerer, message, quantity, meetsTTSThreshold)),
            approved: approved);
    }

    protected virtual Task<string?> GetCheerChatResponse(
        Core.Database.User cheerer,
        string message,
        int quantity)
    {
        return Task.FromResult<string?>(null);
    }

    protected virtual async Task<AudioRequest?> GetCheerAudioRequest(
        Core.Database.User cheerer,
        string message,
        int quantity,
        bool meetsTTSThreshold)
    {
        AudioRequest? soundEffectRequest = null;
        AudioRequest? ttsRequest = null;

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

        if (meetsTTSThreshold && !string.IsNullOrWhiteSpace(message))
        {
            ttsRequest = await ttsRenderer.TTSRequest(
                authorizationLevel: cheerer.AuthorizationLevel,
                voicePreference: cheerer.TTSVoicePreference,
                pitchPreference: cheerer.TTSPitchPreference,
                speedPreference: cheerer.TTSSpeedPreference,
                effectsChain: audioEffectSystem.SafeParse(cheerer.TTSEffectsChain),
                ttsText: message);
        }

        return AudioTools.JoinRequests(300, soundEffectRequest, ttsRequest);
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

        string? chatResponse = await GetRaidChatResponse(raider, count);
        if (!string.IsNullOrWhiteSpace(chatResponse))
        {
            communication.SendPublicChatMessage(chatResponse);
        }

        activityDispatcher.QueueActivity(
            activity: new AudioOnlyActivityRequest(
                activityHandler: this,
                description: $"Raid: {raider} with {count} viewers",
                requesterId: raider.TwitchUserId,
                audioRequest: await GetRaidAudioRequest(raider, count)),
            approved: approved);
    }

    protected virtual Task<string> GetRaidChatResponse(
        Core.Database.User raider,
        int count)
    {
        return Task.FromResult($"Wow! {raider.TwitchUserName} has Raided with {count} viewers! PogChamp");
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
            activity: new AudioOnlyActivityRequest(
                activityHandler: this,
                description: $"Gift Sub To: {recipientId}",
                requesterId: sender.TwitchUserId,
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
            activity: new AudioOnlyActivityRequest(
                activityHandler: this,
                description: $"Anon Gift Sub To: {recipient}",
                requesterId: "",
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

        string chatResponse = await GetFollowChatResponse(follower);
        if (!string.IsNullOrWhiteSpace(chatResponse))
        {
            communication.SendPublicChatMessage(chatResponse);
        }

        activityDispatcher.QueueActivity(
            activity: new AudioOnlyActivityRequest(
                activityHandler: this,
                description: $"Follower: {follower.TwitchUserName}",
                requesterId: follower.TwitchUserId,
                audioRequest: await GetFollowAudioRequest(follower)),
            approved: approved);
    }

    protected virtual Task<string> GetFollowChatResponse(Core.Database.User follower)
    {
        return Task.FromResult($"Thanks for following, @{follower.TwitchUserName}");
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

    #endregion IFollowerHandler
    #region ITTSHandler

    bool ITTSHandler.IsTTSVoiceValid(string voice) => ttsRenderer.IsTTSVoiceValid(voice);
    TTSVoiceInfo? ITTSHandler.GetTTSVoiceInfo(string voice) => ttsRenderer.GetTTSVoiceInfo(voice);

    Task<bool> ITTSHandler.SetTTSEnabled(bool enabled) => ttsRenderer.SetTTSEnabled(enabled);

    public virtual async void HandleTTS(
        Core.Database.User user,
        string message,
        bool approved)
    {
        string? chatResponse = await GetTTSChatResponse(user, message);
        if (!string.IsNullOrWhiteSpace(chatResponse))
        {
            communication.SendPublicChatMessage(chatResponse);
        }

        activityDispatcher.QueueActivity(
            activity: new AudioOnlyActivityRequest(
                activityHandler: this,
                description: $"TTS {user.TwitchUserName} : {message}",
                requesterId: user.TwitchUserId,
                audioRequest: await GetTTSAudioRequest(user, message)),
            approved: approved);
    }

    protected virtual Task<string?> GetTTSChatResponse(
        Core.Database.User user,
        string message)
    {
        return Task.FromResult<string?>(null);
    }

    protected virtual Task<AudioRequest?> GetTTSAudioRequest(
        Core.Database.User user,
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

    public class AudioOnlyActivityRequest : ActivityRequest, IAudioActivity
    {
        public AudioRequest? AudioRequest { get; }

        public AudioOnlyActivityRequest(
            IActivityHandler activityHandler,
            string description,
            string requesterId,
            AudioRequest? audioRequest)
            : base(activityHandler, description, requesterId)
        {
            AudioRequest = audioRequest;
        }
    }
}
