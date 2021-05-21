using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TASagentTwitchBot.BasicMicController
{
    public class AudioConfigurator : Core.BaseConfigurator
    {
        public AudioConfigurator(
            Core.Config.BotConfiguration botConfig,
            Core.ICommunication communication,
            Core.ErrorHandler errorHandler)
            : base(botConfig, communication, errorHandler)
        {
        }

        public override Task<bool> VerifyConfigured()
        {
            bool successful = true;

            successful |= ConfigurePasswords();

            successful |= ConfigureAudioOutputDevices();
            successful |= ConfigureAudioInputDevices();

            return Task.FromResult(successful);
        }
    }
}
