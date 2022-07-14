using TASagentTwitchBot.Core.Commands;

namespace TASagentTwitchBot.SimpleDemo.PointsSpender;

public class PointsSpenderSystem : ICommandContainer
{
    private readonly PointSpenderConfiguration pointSpenderConfig;
    private readonly Core.ICommunication communication;
    private readonly IPointSpenderHandler pointsSpenderHandler;

    private DateTime lastLeaderboardRequest = DateTime.MinValue;
    private readonly TimeSpan leaderboardCooldown = new TimeSpan(0, 1, 0);

    public PointsSpenderSystem(
        PointSpenderConfiguration pointSpenderConfig,
        Core.ICommunication communication,
        IPointSpenderHandler pointsSpenderHandler)
    {
        this.pointSpenderConfig = pointSpenderConfig;
        this.communication = communication;
        this.pointsSpenderHandler = pointsSpenderHandler;
    }

    public void RegisterCommands(ICommandRegistrar commandRegistrar)
    {
        if (!pointSpenderConfig.Enabled)
        {
            //Don't bind commands if disabled
            return;
        }

        if (!string.IsNullOrEmpty(pointSpenderConfig.PointsCommand))
        {
            commandRegistrar.RegisterGlobalCommand(pointSpenderConfig.PointsCommand, PointsHandler);
        }

        if (!string.IsNullOrEmpty(pointSpenderConfig.LeaderboardCommand))
        {
            commandRegistrar.RegisterGlobalCommand(pointSpenderConfig.LeaderboardCommand, LeaderboardHandler);
        }
    }

    public IEnumerable<string> GetPublicCommands()
    {
        if (!pointSpenderConfig.Enabled)
        {
            //Don't return commands if disabled
            yield break;
        }

        if (!string.IsNullOrEmpty(pointSpenderConfig.PointsCommand))
        {
            yield return pointSpenderConfig.PointsCommand.ToLower();
        }

        if (!string.IsNullOrEmpty(pointSpenderConfig.LeaderboardCommand))
        {
            yield return pointSpenderConfig.LeaderboardCommand.ToLower();
        }
    }

    private async Task PointsHandler(Core.IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (remainingCommand.Length > 1)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, incorrectly formatted {pointSpenderConfig.PointsCommand} command. " +
                $"Use either \"!{pointSpenderConfig.PointsCommand}\" or \"!{pointSpenderConfig.PointsCommand} @user\".");
        }
        else if (remainingCommand.Length == 0)
        {
            await pointsSpenderHandler.PrintSelfPoints(chatter.User);
        }
        else
        {
            string otherUserName = remainingCommand[0];

            if (otherUserName[0] == '@')
            {
                otherUserName = otherUserName[1..];
            }

            await pointsSpenderHandler.PrintOtherPoints(chatter.User, otherUserName);
        }
    }

    private async Task LeaderboardHandler(Core.IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (DateTime.Now - lastLeaderboardRequest > leaderboardCooldown)
        {
            lastLeaderboardRequest = DateTime.Now;
            await pointsSpenderHandler.PrintLeaderboard();
        }
    }
}
