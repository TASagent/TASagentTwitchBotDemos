namespace TASagentTwitchBot.EmoteEffectsDemo;

public class EmoteEffectsDemoConfigurator : Core.StandardConfigurator
{
    public EmoteEffectsDemoConfigurator(
        Core.Config.BotConfiguration botConfig,
        Core.ICommunication communication,
        Core.ErrorHandler errorHandler,
        Core.API.Twitch.HelixHelper helixHelper,
        Core.API.Twitch.IBotTokenValidator botTokenValidator,
        Core.API.Twitch.IBroadcasterTokenValidator broadcasterTokenValidator)
        : base(
            botConfig,
            communication,
            errorHandler,
            helixHelper,
            botTokenValidator,
            broadcasterTokenValidator)
    {

    }

    public override async Task<bool> VerifyConfigured()
    {
        bool successful = true;

        //Client Information
        successful |= ConfigureTwitchClient();

        //Check Accounts
        successful |= await ConfigureBotAccount(botTokenValidator);
        successful |= await ConfigureBroadcasterAccount(broadcasterTokenValidator, helixHelper);

        successful |= ConfigurePasswords();

        return successful;
    }
}
