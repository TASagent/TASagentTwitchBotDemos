using TASagentTwitchBot.Core.Commands;

namespace TASagentTwitchBot.SimpleDemo.PointsSpender;

public class PointsSpenderSystem : ICommandContainer
{
    private readonly Core.ICommunication communication;
    private readonly IPointSpenderHandler pointsSpenderHandler;

    private DateTime lastLeaderboardRequest = DateTime.MinValue;
    private readonly TimeSpan leaderboardCooldown = new TimeSpan(0, 1, 0);

    public PointsSpenderSystem(
        Core.ICommunication communication,
        IPointSpenderHandler pointsSpenderHandler)
    {
        this.communication = communication;
        this.pointsSpenderHandler = pointsSpenderHandler;
    }

    public void RegisterCommands(ICommandRegistrar commandRegistrar)
    {
        commandRegistrar.RegisterGlobalCommand("points", PointsHandler);
        commandRegistrar.RegisterGlobalCommand("leaderboard", LeaderboardHandler);
    }

    public IEnumerable<string> GetPublicCommands()
    {
        yield return "points";
        yield return "leaderboard";
    }

    private async Task PointsHandler(Core.IRC.TwitchChatter chatter, string[] remainingCommand)
    {
        if (remainingCommand.Length > 1)
        {
            communication.SendPublicChatMessage($"@{chatter.User.TwitchUserName}, incorrectly formatted points request.");
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
