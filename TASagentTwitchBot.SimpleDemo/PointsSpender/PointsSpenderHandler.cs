using System.Text;
using System.Text.Json;
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
    private readonly HelixHelper helixHelper;
    private readonly Core.ICommunication communication;

    private readonly IServiceScopeFactory scopeFactory;

    private readonly SemaphoreSlim initSemaphore = new SemaphoreSlim(1);

    private PointSpenderData pointSpenderData;
    private readonly string dataFilePath;

    private bool initialized = false;
    private bool disposedValue;

    public PointSpenderHandler(
        Core.ICommunication communication,
        HelixHelper helixHelper,
        IServiceScopeFactory scopeFactory)
    {
        this.communication = communication;
        this.helixHelper = helixHelper;
        this.scopeFactory = scopeFactory;

        dataFilePath = BGC.IO.DataManagement.PathForDataFile("Config", "PointSpender.json");

        if (File.Exists(dataFilePath))
        {
            pointSpenderData = JsonSerializer.Deserialize<PointSpenderData>(File.ReadAllText(dataFilePath))!;
        }
        else
        {
            pointSpenderData = new PointSpenderData("");
            File.WriteAllText(dataFilePath, JsonSerializer.Serialize(pointSpenderData));
        }
    }

    public async Task RegisterHandler(Dictionary<string, RedemptionHandler> handlers)
    {
        if (!initialized)
        {
            await InitializeAsync();
        }

        handlers.Add(pointSpenderData.PointSpenderID, HandleRedemption);
    }

    private async Task Initialize()
    {
        if (!string.IsNullOrEmpty(pointSpenderData.PointSpenderID))
        {
            //Verify
            TwitchCustomReward customRewards = await helixHelper.GetCustomReward(
                id: pointSpenderData.PointSpenderID,
                onlyManageableRewards: true) ??
                throw new Exception($"Unable to get PoinstSpender CustomReward");

            if (customRewards.Data is not null && customRewards.Data.Count == 1 && customRewards.Data[0].Cost == 10_000)
            {
                //Confirmed
            }
            else
            {
                //Error!
                pointSpenderData = new PointSpenderData("");

                communication.SendErrorMessage($"Mismatch for PointSpender Reward");
            }
        }

        if (string.IsNullOrEmpty(pointSpenderData.PointSpenderID))
        {
            //Find or Create
            TwitchCustomReward customRewards = await helixHelper.GetCustomReward(onlyManageableRewards: true) ??
                throw new Exception($"Unable to get PoinstSpender CustomReward");

            string? pointSpenderID = customRewards.Data.FirstOrDefault(x => x.Title.Contains("Spend Channel Points") && x.Cost == 10_000)?.Id;

            if (string.IsNullOrEmpty(pointSpenderID))
            {
                TwitchCustomReward creationResponse = await helixHelper.CreateCustomReward(
                    title: "Spend Channel Points",
                    cost: 10_000,
                    prompt: "Spend your channel points and watch your investment grow.",
                    enabled: true,
                    backgroundColor: "#56BDE6",
                    userInputRequired: false,
                    skipQueue: false) ??
                    throw new Exception($"Unable to create PoinstSpender CustomReward");

                pointSpenderID = creationResponse.Data[0].Id;

                communication.SendDebugMessage($"Created PointSpender Reward");
            }

            pointSpenderData = new PointSpenderData(pointSpenderID);
            Serialize();
        }

        //Handle pending, unfulfilled rewards
        TwitchCustomRewardRedemption pendingRedemptions = await helixHelper.GetCustomRewardRedemptions(
            rewardId: pointSpenderData.PointSpenderID,
            status: "UNFULFILLED") ??
            throw new Exception($"Unable to get PoinstSpender Unfulfilled Rewards");


        bool updated = false;

        using IServiceScope scope = scopeFactory.CreateScope();
        DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        foreach (TwitchCustomRewardRedemption.Datum redemption in pendingRedemptions.Data)
        {
            User? user = await db.Users.FirstOrDefaultAsync(x => x.TwitchUserId == redemption.UserID);

            if (user is null)
            {
                communication.SendErrorMessage($"User not found: {redemption.UserID}");
                continue;
            }

            SupplementalData supplementalData = await db.GetSupplementalDataAsync(user);

            updated = true;

            supplementalData.PointsSpent += 10;
            supplementalData.LastPointsSpentUpdate = redemption.RedeemedAt;

            await helixHelper.UpdateCustomRewardRedemptions(
                redemption.RewardData.Id,
                redemption.Id,
                status: "FULFILLED");
        }

        if (updated)
        {
            await db.SaveChangesAsync();

            long totalPointsSpent = 1000 * db.SupplementalData.Select(x => (long)x.PointsSpent).Sum();

            communication.SendPublicChatMessage(
                $"An updated total of {totalPointsSpent:N0} spent channel points.");
        }

    }

    private void Serialize()
    {
        lock (pointSpenderData)
        {
            File.WriteAllText(dataFilePath, JsonSerializer.Serialize(pointSpenderData));
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
        communication.SendDebugMessage($"Redemption: {user.TwitchUserName}");

        using IServiceScope scope = scopeFactory.CreateScope();
        DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        SupplementalData supplementalData = await db.GetSupplementalDataAsync(user);

        supplementalData.PointsSpent += 10;
        supplementalData.LastPointsSpentUpdate = redemption.RedeemedAt;

        await db.SaveChangesAsync();

        await helixHelper.UpdateCustomRewardRedemptions(
            redemption.Reward.Id,
            redemption.Id,
            status: "FULFILLED");

        long totalPointsSpent = 1000 * db.SupplementalData.Select(x => (long)x.PointsSpent).Sum();

        int leaderboardPlacement = db.SupplementalData
            .Include(x => x.User)
            .AsEnumerable()
            .Where(x => x.PointsSpent > 0)
            .OrderByDescending(x => x.PointsSpent)
            .ThenBy(x => x.LastPointsSpentUpdate ?? new DateTime(2020, 1, 1))
            .Select((data, index) => new { data, index })
            .First(x => x.data.SupplementalDataId == supplementalData.SupplementalDataId).index + 1;

        communication.SendPublicChatMessage(
            $"{user.TwitchUserName} has now spent {1000 * supplementalData.PointsSpent:N0} channel points, " +
            $"putting them in {leaderboardPlacement.AsPlacement()} place and making a grand total of {totalPointsSpent:N0} spent channel points.");
    }

    public async Task PrintLeaderboard()
    {
        if (!initialized)
        {
            await InitializeAsync();
        }

        const int messageLimit = 500;

        using IServiceScope scope = scopeFactory.CreateScope();
        DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        long totalPointsSpent = 1000 * db.SupplementalData.Select(x => (long)x.PointsSpent).Sum();

        var topUsers = db.SupplementalData
            .Include(x => x.User)
            .Where(x => x.PointsSpent > 0)
            .OrderByDescending(x => x.PointsSpent)
            .ThenBy(x => x.LastPointsSpentUpdate ?? new DateTime(2020, 1, 1))
            .Take(5)
            .ToList();

        StringBuilder leaderboard = new StringBuilder($"A total of {totalPointsSpent:N0} Channel Points have been spent. Spending leaders: ");

        for (int i = 0; i < topUsers.Count; i++)
        {
            string spentAmount;

            if (topUsers[i].PointsSpent < 1000)
            {
                spentAmount = $"({topUsers[i].PointsSpent}k)";
            }
            else
            {
                spentAmount = $"({topUsers[i].PointsSpent / 1000.0:F1}M)";
            }

            string newSpendMessage = $" {i + 1}. {topUsers[i].User.TwitchUserName} {spentAmount}";

            if (leaderboard.Length + newSpendMessage.Length > messageLimit)
            {
                //Bail if message would be too long
                break;
            }

            leaderboard.Append(newSpendMessage);
        }

        communication.SendPublicChatMessage(leaderboard.ToString());
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

        if (supplementalData.PointsSpent > 0)
        {
            long totalPointsSpent = 1000 * db.SupplementalData.Select(x => (long)x.PointsSpent).Sum();

            int placement = db.SupplementalData
                .Include(x => x.User)
                .AsEnumerable()
                .Where(x => x.PointsSpent > 0)
                .OrderByDescending(x => x.PointsSpent)
                .ThenBy(x => x.LastPointsSpentUpdate ?? new DateTime(2020, 1, 1))
                .Select((data, index) => new { data, index })
                .First(x => x.data.SupplementalDataId == supplementalData.SupplementalDataId).index + 1;

            communication.SendPublicChatMessage(
                $"@{user.TwitchUserName}, you have spent {supplementalData.PointsSpent * 1000:N0} channel points " +
                $"of the cumulative {totalPointsSpent:N0} that have been spent on this redemption, " +
                $"putting you in {placement.AsPlacement()} place on the leaderboard.");
        }
        else
        {
            communication.SendPublicChatMessage($"@{user.TwitchUserName}, you have never spent any channel points with the Spend Channel Points redemption.");
            return;
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

        User? otherUser = await db.Users.FirstOrDefaultAsync(x => x.TwitchUserName.ToLower() == otherUserName);

        if (otherUser is null)
        {
            communication.SendPublicChatMessage($"@{requester.TwitchUserName}, I cannot find anyone named {otherUserName}.");
            return;
        }

        SupplementalData otherUserData = await db.GetSupplementalDataAsync(otherUser);

        long totalPointsSpent = 1000 * db.SupplementalData.Select(x => (long)x.PointsSpent).Sum();

        int placement = db.SupplementalData
            .Include(x => x.User)
            .AsEnumerable()
            .Where(x => x.PointsSpent > 0)
            .OrderByDescending(x => x.PointsSpent)
            .ThenBy(x => x.LastPointsSpentUpdate ?? new DateTime(2020, 1, 1))
            .Select((data, index) => new { data, index })
            .First(x => x.data.SupplementalDataId == otherUserData.SupplementalDataId).index + 1;

        if (otherUser == requester)
        {
            //Someone targeted themselves
            communication.SendPublicChatMessage(
                $"@{requester.TwitchUserName}, you have spent {otherUserData.PointsSpent * 1000:N0} Channel Points " +
                $"of the cumulative {totalPointsSpent:N0} that have been spent " +
                $"(putting you in {placement.AsPlacement()} place on the leaderboard).");

            return;
        }

        communication.SendPublicChatMessage(
            $"@{requester.TwitchUserName}, it seems {otherUser.TwitchUserName} has spent {otherUserData.PointsSpent * 1000:N0} Channel Points " +
            $"of the cumulative {totalPointsSpent:N0} that have been spent " +
            $"(putting them in {placement.AsPlacement()} place on the leaderboard).");
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


    private record PointSpenderData(string PointSpenderID);
}
