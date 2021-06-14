using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TASagentTwitchBot.TTTASDemo
{
    public class TTTASDemoApplication
    {
        private readonly Core.ICommunication communication;
        private readonly Core.IMessageAccumulator messageAccumulator;
        private readonly Core.ErrorHandler errorHandler;
        private readonly Core.ApplicationManagement applicationManagement;

        private readonly Core.API.Twitch.IBotTokenValidator botTokenValidator;
        private readonly Core.API.Twitch.IBroadcasterTokenValidator broadcasterTokenValidator;
        private readonly Core.PubSub.PubSubClient pubSubClient;
        private readonly Core.IRC.IrcClient ircClient;

        private readonly Core.Audio.IMicrophoneHandler microphoneHandler;

        public TTTASDemoApplication(
            Core.ICommunication communication,
            Core.IMessageAccumulator messageAccumulator,
            Core.ErrorHandler errorHandler,
            Core.ApplicationManagement applicationManagement,
            Core.IRC.IrcClient ircClient,
            Core.API.Twitch.IBotTokenValidator botTokenValidator,
            Core.API.Twitch.IBroadcasterTokenValidator broadcasterTokenValidator,
            Core.PubSub.PubSubClient pubSubClient,
            Core.Audio.IMicrophoneHandler microphoneHandler)
        {
            this.communication = communication;
            this.messageAccumulator = messageAccumulator;
            this.errorHandler = errorHandler;
            this.applicationManagement = applicationManagement;
            this.ircClient = ircClient;
            this.botTokenValidator = botTokenValidator;
            this.broadcasterTokenValidator = broadcasterTokenValidator;
            this.pubSubClient = pubSubClient;
            this.microphoneHandler = microphoneHandler;
        }

        public async Task RunAsync()
        {
            try
            {
                communication.SendDebugMessage("*** Starting Up TTTAS Application ***");

                microphoneHandler.Start();

                communication.SendDebugMessage("Connecting to IRC");

                await ircClient.Start();

                //Kick off Validators
                botTokenValidator.RunValidator();
                broadcasterTokenValidator.RunValidator();

                communication.SendPublicChatMessage("I have connected.");
            }
            catch (Exception ex)
            {
                errorHandler.LogFatalException(ex);
            }

            messageAccumulator.MonitorMessages();

            await pubSubClient.Launch();

            try
            {
                await applicationManagement.WaitForEndAsync();
            }
            catch (Exception ex)
            {
                errorHandler.LogSystemException(ex);
            }


            //Handle Cleanup
            try
            {
                microphoneHandler.Dispose();
            }
            catch (Exception ex)
            {
                errorHandler.LogSystemException(ex);
            }

            try
            {
                ircClient.Dispose();
            }
            catch (Exception ex)
            {
                errorHandler.LogSystemException(ex);
            }

            try
            {
                pubSubClient.Dispose();
            }
            catch (Exception ex)
            {
                errorHandler.LogSystemException(ex);
            }
        }
    }
}
