using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TASagentTwitchBot.TTTASDemo
{
    public class TTTASConfigurator : Core.StandardConfigurator
    {
        public TTTASConfigurator(
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

            successful |= ConfigureAudioOutput();
            successful |= ConfigureAudioInputDevices();

            return successful;
        }


        private bool ConfigureAudioOutput()
        {
            bool successful = true;

            //Set Audio Devices
            if (string.IsNullOrEmpty(botConfig.EffectOutputDevice) || string.IsNullOrEmpty(botConfig.VoiceOutputDevice))
            {
                List<string> devices = GetAudioOutputDevicesList();

                Console.WriteLine($"Detected, Active Output devices:");

                for (int i = 0; i < devices.Count; i++)
                {
                    Console.WriteLine($"  {i}) {devices[i]}");
                }
                Console.WriteLine();

                if (string.IsNullOrEmpty(botConfig.EffectOutputDevice))
                {
                    WritePrompt($"Default Text-To-TAS Output Device Number");
                    string inputLine = Console.ReadLine();
                    Console.WriteLine();

                    if (int.TryParse(inputLine, out int value))
                    {
                        if (value >= 0 && value < devices.Count)
                        {
                            botConfig.EffectOutputDevice = devices[value];
                            botConfig.Serialize();
                        }
                        else
                        {
                            WriteError("Value out of range.");
                            successful = false;
                        }
                    }
                    else
                    {
                        WriteError("Unable to parse value.");
                        successful = false;
                    }

                    Console.WriteLine();
                }

                if (string.IsNullOrEmpty(botConfig.VoiceOutputDevice))
                {
                    botConfig.CommandConfiguration.EnableErrorHandling = false;
                    botConfig.CommandConfiguration.HelpEnabled = false;
                    botConfig.CommandConfiguration.SetEnabled = false;
                    botConfig.CommandConfiguration.GetEnabled = false;

                    botConfig.MicConfiguration.Enabled = false;
                    botConfig.VoiceOutputDevice = devices[0];
                    botConfig.Serialize();
                }

                Console.WriteLine();
            }

            return successful;
        }
    }
}
