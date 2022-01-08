using TASagentTwitchBot.Core.Commands;

namespace TASagentTwitchBot.SimpleDemo.Commands;

public class TestCommandSystem : ICommandContainer
{
    private readonly Core.ICommunication communication;
    private readonly Notifications.CustomActivityProvider customActivityProvider;

    public TestCommandSystem(
        Core.ICommunication communication,
        Notifications.CustomActivityProvider customActivityProvider)
    {
        this.communication = communication;
        this.customActivityProvider = customActivityProvider;
    }

    public void RegisterCommands(ICommandRegistrar commandRegistrar)
    {
        commandRegistrar.RegisterGlobalCommand("test", TestRunHandler);
    }

    public IEnumerable<string> GetPublicCommands()
    {
        yield break;
    }

    private Task TestRunHandler(Core.IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (chatter.User.AuthorizationLevel < AuthorizationLevel.Admin)
        {
            communication.SendPublicChatMessage($"You are not authorized to test notifications, @{chatter.User.TwitchUserName}.");
            return Task.CompletedTask;
        }

        customActivityProvider.TestNotification();

        return Task.CompletedTask;
    }
}
