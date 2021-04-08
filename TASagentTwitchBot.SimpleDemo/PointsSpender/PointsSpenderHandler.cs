using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using TASagentTwitchBot.Core.Database;
using TASagentTwitchBot.Core.API.Twitch;
using TASagentTwitchBot.Core.PubSub;

namespace TASagentTwitchBot.SimpleDemo.PointsSpender
{
    public interface IPointSpenderHandler : IRedemptionContainer
    {
        Task PrintLeaderboard();
        Task PrintSelfPoints(User user);
        Task PrintOtherPoints(User requester, string otherUserName);
    }

    public class PointSpenderHandler : IPointSpenderHandler, IRedemptionContainer, IDisposable
    {
        private readonly Database.DatabaseContext db;
        private readonly HelixHelper helixHelper;
        private readonly Core.ICommunication communication;

        private readonly SemaphoreSlim initSemaphore = new SemaphoreSlim(1);

        private PointSpenderData pointSpenderData;
        private readonly string dataFilePath;

        private bool initialized = false;
        private bool disposedValue;

        public PointSpenderHandler(
            Core.ICommunication communication,
            HelixHelper helixHelper,
            Database.DatabaseContext db)
        {
            this.communication = communication;
            this.helixHelper = helixHelper;
            this.db = db;

            dataFilePath = BGC.IO.DataManagement.PathForDataFile("Config", "PointSpender.json");

            if (File.Exists(dataFilePath))
            {
                pointSpenderData = JsonSerializer.Deserialize<PointSpenderData>(File.ReadAllText(dataFilePath));
            }
            else
            {
                pointSpenderData = new PointSpenderData(null);
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

                var customRewards = await helixHelper.GetCustomReward(
                    id: pointSpenderData.PointSpenderID,
                    onlyManageableRewards: true);

                if (customRewards.Data.Count == 1 && customRewards.Data[0].Cost == 10_000)
                {
                    //Confirmed
                }
                else
                {
                    //Error!
                    pointSpenderData = new PointSpenderData(null);

                    communication.SendErrorMessage($"Mismatch for PointSpender Reward");
                }
            }

            if (string.IsNullOrEmpty(pointSpenderData.PointSpenderID))
            {
                //Find or Create

                var customRewards = await helixHelper.GetCustomReward(onlyManageableRewards: true);

                string pointSpenderID = "";

                foreach (var customReward in customRewards.Data)
                {
                    if (customReward.Title.Contains("Spend Channel Points") && customReward.Cost == 10_000)
                    {
                        pointSpenderID = customReward.Id;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(pointSpenderID))
                {
                    var creationResponse = await helixHelper.CreateCustomReward(
                        title: "Spend Channel Points",
                        cost: 10_000,
                        prompt: "Spend your channel points and watch your investment grow.",
                        enabled: true,
                        backgroundColor: "#56BDE6",
                        userInputRequired: false,
                        maxPerStreamEnabled: false,
                        maxPerUserPerStreamEnabled: false,
                        globalCooldownEnabled: false,
                        redemptionsSkipQueue: false);

                    pointSpenderID = creationResponse.Data[0].Id;

                    communication.SendDebugMessage($"Created PointSpender Reward");
                }

                pointSpenderData = new PointSpenderData(pointSpenderID);
                Serialize();
            }

            //Handle pending, unfulfilled rewards
            var pendingRedemptions = await helixHelper.GetCustomRewardRedemptions(
                rewardId: pointSpenderData.PointSpenderID,
                status: "UNFULFILLED");

            bool updated = false;

            foreach (var redemption in pendingRedemptions.Data)
            {
                User user = db.Users.First(x => x.TwitchUserId == redemption.UserID);

                if (user is null)
                {
                    communication.SendErrorMessage($"User not found: {redemption.UserID}");
                    continue;
                }

                Database.SupplementalData supplementalData = await db.GetSupplementalDataAsync(user);

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

            Database.SupplementalData supplementalData = await db.GetSupplementalDataAsync(user);

            supplementalData.PointsSpent += 10;
            supplementalData.LastPointsSpentUpdate = redemption.RedeemedAt;

            await db.SaveChangesAsync();

            await helixHelper.UpdateCustomRewardRedemptions(
                redemption.Reward.Id,
                redemption.Id,
                status: "FULFILLED");

            long totalPointsSpent = 1000 * db.SupplementalData.Select(x => (long)x.PointsSpent).Sum();

            communication.SendPublicChatMessage(
                $"A grand total of {totalPointsSpent:N0} Channel Points have been spent.");
        }

        public async Task PrintLeaderboard()
        {
            if (!initialized)
            {
                await InitializeAsync();
            }

            const int messageLimit = 500;

            long totalPointsSpent = 1000 * db.SupplementalData.Select(x => (long)x.PointsSpent).Sum();

            var topUsers = db.SupplementalData
                .Include(x => x.User)
                .Where(x => x.PointsSpent > 0)
                .OrderByDescending(x => x.PointsSpent)
                .ThenBy(x => x.LastPointsSpentUpdate ?? new DateTime(2020, 1, 1)).Take(5).ToList();

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

            Database.SupplementalData supplementalData = await db.GetSupplementalDataAsync(user);

            if (supplementalData.PointsSpent > 0)
            {
                communication.SendPublicChatMessage(
                    $"@{user.TwitchUserName}, you have spent {supplementalData.PointsSpent * 1000:N0} Channel Points.");
            }
            else
            {
                communication.SendPublicChatMessage($"@{user.TwitchUserName}, you have never spent any Channel Points.");
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

            User otherUser = db.Users.FirstOrDefault(x => x.TwitchUserName.ToLower() == otherUserName);

            if (otherUser == null)
            {
                communication.SendPublicChatMessage($"@{requester.TwitchUserName}, I cannot find anyone named {otherUserName}.");
                return;
            }

            Database.SupplementalData otherUserData = await db.GetSupplementalDataAsync(otherUser);

            if (otherUser == requester)
            {
                //Someone targeted themselves
                communication.SendPublicChatMessage(
                    $"@{requester.TwitchUserName}, you have spent {otherUserData.PointsSpent * 1000:N0} Channel Points.");

                return;
            }

            communication.SendPublicChatMessage(
                $"@{requester.TwitchUserName}, it seems {otherUser.TwitchUserName} has spent {otherUserData.PointsSpent * 1000:N0} Channel Points.");
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
}
