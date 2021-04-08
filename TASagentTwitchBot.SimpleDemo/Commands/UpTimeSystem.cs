using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using TASagentTwitchBot.Core.Commands;

namespace TASagentTwitchBot.SimpleDemo.Commands
{
    public class UpTimeSystem : ICommandContainer
    {
        private readonly Core.Config.BotConfiguration botConfig;
        private readonly Core.ICommunication communication;
        private readonly Core.API.Twitch.HelixHelper helixHelper;

        public UpTimeSystem(
            Core.Config.IBotConfigContainer botConfigContainer,
            Core.ICommunication communication,
            Core.API.Twitch.HelixHelper helixHelper)
        {
            botConfig = botConfigContainer.BotConfig;
            this.communication = communication;
            this.helixHelper = helixHelper;
        }

        public void RegisterCommands(
            Dictionary<string, CommandHandler> commands,
            Dictionary<string, HelpFunction> helpFunctions,
            Dictionary<string, SetFunction> setFunctions)
        {
            commands.Add("uptime", UpTime);
        }

        public IEnumerable<string> GetPublicCommands()
        {
            yield return "uptime";
        }

        private async Task UpTime(
            Core.IRC.TwitchChatter chatter,
            string[] remainingCommand)
        {
            await PrintUpTime();
        }

        public async Task PrintUpTime()
        {
            Core.API.Twitch.TwitchStreams streamResults = await helixHelper.GetStreams(userIDs: new List<string>() { botConfig.BroadcasterId });

            if (streamResults is null || streamResults.Data is null || streamResults.Data.Count == 0)
            {
                communication.SendPublicChatMessage($"This channel is not currently live.");
                return;
            }

            TimeSpan timeDiff = DateTime.Now - streamResults.Data[0].StartedAt;

            if (timeDiff.Hours > 0)
            {
                communication.SendPublicChatMessage($"This channel has been live for {timeDiff.Hours} Hour(s) and {timeDiff.Minutes} Minute(s)");
            }
            else
            {
                communication.SendPublicChatMessage($"This channel has been live for {timeDiff.Minutes} Minute(s)");
            }
        }
    }
}
