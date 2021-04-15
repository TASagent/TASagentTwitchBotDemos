using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TASagentTwitchBot.BasicMicController
{
    public class AudioConfigurator : Core.BaseConfigurator
    {
        public AudioConfigurator(
            Core.Config.IBotConfigContainer botConfigContainer,
            Core.ICommunication communication,
            Core.ErrorHandler errorHandler)
            : base(botConfigContainer, communication, errorHandler)
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
