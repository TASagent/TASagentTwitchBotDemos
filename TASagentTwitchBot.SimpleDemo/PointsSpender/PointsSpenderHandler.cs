using System.Text;
using Microsoft.EntityFrameworkCore;
using BGC.Utility;

using TASagentTwitchBot.Core.Database;
using TASagentTwitchBot.Core.API.Twitch;
using TASagentTwitchBot.Core.PubSub;
using TASagentTwitchBot.SimpleDemo.Database;

namespace TASagentTwitchBot.SimpleDemo.PointsSpender;

[Core.AutoRegister]
public interface IPointSpenderHandler : IRedemptionContainer
{
    Task PrintLeaderboard();
    Task PrintSelfPoints(User user);
    Task PrintOtherPoints(User requester, string otherUserName);
}

public class PointSpenderHandler : IPointSpenderHandler, IRedemptionContainer, IDisposable
{
    private readonly PointSpenderConfiguration pointSpenderConfig;
    private readonly Core.ICommunication communication;

    private readonly HelixHelper helixHelper;
    private readonly IServiceScopeFactory scopeFactory;

    private readonly SemaphoreSlim initSemaphore = new SemaphoreSlim(1);

    private bool initialized = false;
    private bool disposedValue;

    public PointSpenderHandler(
        PointSpenderConfiguration pointSpenderConfig,
        Core.ICommunication communication,
        HelixHelper helixHelper,
        IServiceScopeFactory scopeFactory)
    {
        this.pointSpenderConfig = pointSpenderConfig;
        this.communication = communication;
        this.helixHelper = helixHelper;
        this.scopeFactory = scopeFactory;
    }

    public async Task RegisterHandler(Dictionary<string, RedemptionHandler> handlers)
    {
        if (!initialized)
        {
            await InitializeAsync();
        }

        if (!pointSpenderConfig.Enabled)
        {
            //Don't register handler if we're disabled
            return;
        }

        handlers.Add(pointSpenderConfig.PointSpenderID, HandleRedemption);
    }

    private async Task Initialize()
    {
        if (!string.IsNullOrEmpty(pointSpenderConfig.PointSpenderID))
        {
            //Verify
            TwitchCustomReward customRewards = await helixHelper.GetCustomReward(
                id: pointSpenderConfig.PointSpenderID,
                onlyManageableRewards: true) ??
                throw new Exception($"Unable to get PoinstSpender CustomReward");

            if (customRewards.Data is not null && customRewards.Data.Count == 1)
            {
                //Confirmed
                TwitchCustomReward.Datum redemption = customRewards.Data[0];

                //Verify state is correct
                if (redemption.IsEnabled != pointSpenderConfig.Enabled)
                {
                    await helixHelper.UpdateCustomRewardProperties(
                        id: pointSpenderConfig.PointSpenderID,
                        enabled: pointSpenderConfig.Enabled);
                }
            }
            else
            {
                //Error!
                pointSpenderConfig.PointSpenderID = "";
                pointSpenderConfig.Serialize();

                communication.SendErrorMessage($"Mismatch for PointSpender Reward");
            }
        }

        if (pointSpenderConfig.Enabled && string.IsNullOrEmpty(pointSpenderConfig.PointSpenderID))
        {
            //Create
            TwitchCustomReward creationResponse = await helixHelper.CreateCustomReward(
                title: "Spend Channel Points",
                cost: 10_000,
                prompt: "Spend your channel points and watch your investment grow.",
                enabled: true,
                backgroundColor: "#56BDE6",
                userInputRequired: false,
                skipQueue: false) ??
                throw new Exception($"Unable to create PoinstSpender CustomReward");

            communication.SendDebugMessage($"Created PointSpender Reward");

            pointSpenderConfig.PointSpenderID = creationResponse.Data[0].Id;
            pointSpenderConfig.Serialize();
        }

        if (!string.IsNullOrEmpty(pointSpenderConfig.PointSpenderID))
        {
            //Handle pending, unfulfilled rewards
            TwitchCustomRewardRedemption pendingRedemptions = await helixHelper.GetCustomRewardRedemptions(
                rewardId: pointSpenderConfig.PointSpenderID,
                status: "UNFULFILLED") ??
                throw new Exception($"Unable to get PoinstSpender Unfulfilled Rewards");

            int updatedCount = 0;

            foreach (TwitchCustomRewardRedemption.Datum redemption in pendingRedemptions.Data)
            {
                updatedCount++;

                await helixHelper.UpdateCustomRewardRedemptions(
                    rewardId: redemption.RewardData.Id,
                    id: redemption.Id,
                    status: "CANCELED");
            }

            if (updatedCount > 0)
            {
                communication.SendDebugMessage(
                    $"Refunded {updatedCount} pending PoinstSpender Redemptions.");
            }
        }
    }

    private async Task InitializeAsync()
    {
        if (initialized)
        {
            return;
        }

        await initSemaphore.WaitAsync();

        if (initialized)
        {
            initSemaphore.Release();
            return;
        }

        await Initialize();

        initialized = true;
        initSemaphore.Release();
    }

    public async Task HandleRedemption(User user, ChannelPointMessageData.Datum.RedemptionData redemption)
    {
        //Handle redemption
        communication.SendDebugMessage($"PointSpender Redemption: {user.TwitchUserName}");

        using IServiceScope scope = scopeFactory.CreateScope();
        DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        SupplementalData supplementalData = await db.GetSupplementalDataAsync(user);

        supplementalData.PointsSpent += redemption.Reward.Cost;
        supplementalData.LastPointsSpentUpdate = redemption.RedeemedAt;

        await db.SaveChangesAsync();

        await helixHelper.UpdateCustomRewardRedemptions(
            redemption.Reward.Id,
            redemption.Id,
            status: "FULFILLED");

        if (!string.IsNullOrEmpty(pointSpenderConfig.RedemptionMessage))
        {
            long totalPointsSpent = db.SupplementalData.Select(x => (long)x.PointsSpent).Sum();

            int leaderboardPlacement = db.SupplementalData
                .Include(x => x.User)
                .AsEnumerable()
                .Where(x => x.PointsSpent > 0)
                .OrderByDescending(x => x.PointsSpent)
                .ThenBy(x => x.LastPointsSpentUpdate ?? new DateTime(2020, 1, 1))
                .Select((data, index) => new { data, index })
                .First(x => x.data.SupplementalDataId == supplementalData.SupplementalDataId).index + 1;

            string redemptionMessage = pointSpenderConfig.RedemptionMessage
                .Replace("$REDEEMER_NAME", user.TwitchUserName)
                .Replace("$REDEEMER_POINTS", supplementalData.PointsSpent.ToString("N0"))
                .Replace("$REDEEMER_PLACE", leaderboardPlacement.AsPlacement())
                .Replace("$TOTAL_POINTS", totalPointsSpent.ToString("N0"));

            communication.SendPublicChatMessage(redemptionMessage);
        }
    }

    public async Task PrintLeaderboard()
    {
        if (!initialized)
        {
            await InitializeAsync();
        }

        const int MESSAGE_LIMIT = 500;

        using IServiceScope scope = scopeFactory.CreateScope();
        DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        long totalPointsSpent = db.SupplementalData.Select(x => (long)x.PointsSpent).Sum();

        var topUsers = db.SupplementalData
            .Include(x => x.User)
            .Where(x => x.PointsSpent > 0)
            .OrderByDescending(x => x.PointsSpent)
            .ThenBy(x => x.LastPointsSpentUpdate ?? new DateTime(2020, 1, 1))
            .Take(5)
            .ToList();

        StringBuilder leaderboardDescription = new StringBuilder();

        for (int i = 0; i < topUsers.Count; i++)
        {
            string spentAmount;

            if (topUsers[i].PointsSpent < 1_000)
            {
                spentAmount = $"({topUsers[i].PointsSpent})";
            }
            else if (topUsers[i].PointsSpent < 1_000_000)
            {
                spentAmount = $"({topUsers[i].PointsSpent / 1_000.0:F1}k)";
            }
            else
            {
                spentAmount = $"({topUsers[i].PointsSpent / 1_000_000.0:F1}M)";
            }

            string newSpendMessage = $" {i + 1}. {topUsers[i].User.TwitchUserName} {spentAmount}";

            if (leaderboardDescription.Length + newSpendMessage.Length + pointSpenderConfig.LeaderboardMessage.Length > MESSAGE_LIMIT)
            {
                //Bail if message would be too long
                break;
            }

            leaderboardDescription.Append(newSpendMessage);
        }

        string redemptionMessage = pointSpenderConfig.LeaderboardMessage
                .Replace("$TOTAL_POINTS", totalPointsSpent.ToString("N0"))
                .Replace("$LEADERBOARD", leaderboardDescription.ToString());

        communication.SendPublicChatMessage(redemptionMessage);
    }

    public async Task PrintSelfPoints(User user)
    {
        if (!initialized)
        {
            await InitializeAsync();
        }

        using IServiceScope scope = scopeFactory.CreateScope();
        DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        SupplementalData supplementalData = await db.GetSupplementalDataAsync(user);

        long totalPointsSpent = db.SupplementalData.Select(x => (long)x.PointsSpent).Sum();

        if (supplementalData.PointsSpent > 0)
        {
            int placement = db.SupplementalData
                .Include(x => x.User)
                .AsEnumerable()
                .Where(x => x.PointsSpent > 0)
                .OrderByDescending(x => x.PointsSpent)
                .ThenBy(x => x.LastPointsSpentUpdate ?? new DateTime(2020, 1, 1))
                .Select((data, index) => new { data, index })
                .First(x => x.data.SupplementalDataId == supplementalData.SupplementalDataId).index + 1;

            string pointsSelfMessage = pointSpenderConfig.PointsSelfMessage
                .Replace("$REQUESTER_NAME", user.TwitchUserName)
                .Replace("$TARGET_NAME", user.TwitchUserName)
                .Replace("$TARGET_POINTS", supplementalData.PointsSpent.ToString("N0"))
                .Replace("$TARGET_PLACE", placement.AsPlacement())
                .Replace("$TOTAL_POINTS", totalPointsSpent.ToString("N0"));

            communication.SendPublicChatMessage(pointsSelfMessage);
        }
        else
        {
            string pointsSelfMessage = pointSpenderConfig.PointsSelfNoneMessage
                .Replace("$REQUESTER_NAME", user.TwitchUserName)
                .Replace("$TARGET_NAME", user.TwitchUserName)
                .Replace("$TARGET_POINTS", supplementalData.PointsSpent.ToString("N0"))
                .Replace("$TOTAL_POINTS", totalPointsSpent.ToString("N0"));

            communication.SendPublicChatMessage(pointsSelfMessage);
        }
    }

    public async Task PrintOtherPoints(User requester, string otherUserName)
    {
        if (!initialized)
        {
            await InitializeAsync();
        }

        otherUserName = otherUserName.ToLower();

        using IServiceScope scope = scopeFactory.CreateScope();
        DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        long totalPointsSpent = db.SupplementalData.Select(x => (long)x.PointsSpent).Sum();

        User? otherUser = await db.Users.FirstOrDefaultAsync(x => x.TwitchUserName.ToLower() == otherUserName);

        if (otherUser is null)
        {
            string pointsOtherMessage = pointSpenderConfig.PointsOtherUserNotFound
                .Replace("$REQUESTER_NAME", requester.TwitchUserName)
                .Replace("$TARGET_NAME", otherUserName)
                .Replace("$TOTAL_POINTS", totalPointsSpent.ToString("N0"));

            communication.SendPublicChatMessage(pointsOtherMessage);

            return;
        }

        SupplementalData otherUserData = await db.GetSupplementalDataAsync(otherUser);

        if (otherUserData.PointsSpent > 0)
        {
            //The targeted user had points

            int placement = db.SupplementalData
                .Include(x => x.User)
                .AsEnumerable()
                .Where(x => x.PointsSpent > 0)
                .OrderByDescending(x => x.PointsSpent)
                .ThenBy(x => x.LastPointsSpentUpdate ?? new DateTime(2020, 1, 1))
                .Select((data, index) => new { data, index })
                .First(x => x.data.SupplementalDataId == otherUserData.SupplementalDataId).index + 1;

            if (otherUser != requester)
            {
                //Targeted other user
                string pointsOtherMessage = pointSpenderConfig.PointsOtherMessage
                    .Replace("$REQUESTER_NAME", requester.TwitchUserName)
                    .Replace("$TARGET_NAME", otherUser.TwitchUserName)
                    .Replace("$TARGET_POINTS", otherUserData.PointsSpent.ToString("N0"))
                    .Replace("$TARGET_PLACE", placement.AsPlacement())
                    .Replace("$TOTAL_POINTS", totalPointsSpent.ToString("N0"));

                communication.SendPublicChatMessage(pointsOtherMessage);
            }
            else
            {
                //Targeted self
                string pointsSelfMessage = pointSpenderConfig.PointsSelfMessage
                    .Replace("$REQUESTER_NAME", requester.TwitchUserName)
                    .Replace("$TARGET_NAME", otherUser.TwitchUserName)
                    .Replace("$TARGET_POINTS", otherUserData.PointsSpent.ToString("N0"))
                    .Replace("$TARGET_PLACE", placement.AsPlacement())
                    .Replace("$TOTAL_POINTS", totalPointsSpent.ToString("N0"));

                communication.SendPublicChatMessage(pointsSelfMessage);
            }
        }
        else
        {
            //The targeted user had no points
            if (otherUser != requester)
            {
                //Targeted other user
                string pointsOtherMessage = pointSpenderConfig.PointsOtherNoneMessage
                    .Replace("$REQUESTER_NAME", requester.TwitchUserName)
                    .Replace("$TARGET_NAME", otherUser.TwitchUserName)
                    .Replace("$TARGET_POINTS", otherUserData.PointsSpent.ToString("N0"))
                    .Replace("$TOTAL_POINTS", totalPointsSpent.ToString("N0"));

                communication.SendPublicChatMessage(pointsOtherMessage);
            }
            else
            {
                //Targeted self
                string pointsSelfMessage = pointSpenderConfig.PointsSelfNoneMessage
                    .Replace("$REQUESTER_NAME", requester.TwitchUserName)
                    .Replace("$TARGET_NAME", otherUser.TwitchUserName)
                    .Replace("$TARGET_POINTS", otherUserData.PointsSpent.ToString("N0"))
                    .Replace("$TOTAL_POINTS", totalPointsSpent.ToString("N0"));

                communication.SendPublicChatMessage(pointsSelfMessage);
            }
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                initSemaphore.Dispose();
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
