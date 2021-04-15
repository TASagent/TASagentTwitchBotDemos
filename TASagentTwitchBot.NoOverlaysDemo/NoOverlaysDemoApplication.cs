using System;
using System.Threading.Tasks;

namespace TASagentTwitchBot.NoOverlaysDemo
{
    public class NoOverlaysDemoApplication
    {
        private readonly Core.ICommunication communication;
        private readonly Core.IMessageAccumulator messageAccumulator;
        private readonly Core.ErrorHandler errorHandler;
        private readonly Core.ApplicationManagement applicationManagement;

        private readonly Core.API.Twitch.IBotTokenValidator botTokenValidator;
        private readonly Core.API.Twitch.IBroadcasterTokenValidator broadcasterTokenValidator;
        private readonly Core.WebSub.WebSubHandler webSubHandler;
        private readonly Core.PubSub.PubSubClient pubSubClient;
        private readonly Core.IRC.IrcClient ircClient;

        private readonly Core.Audio.IMicrophoneHandler microphoneHandler;
        private readonly Core.Audio.MidiKeyboardHandler midiKeyboardHandler;

        public NoOverlaysDemoApplication(
            Core.ICommunication communication,
            Core.IMessageAccumulator messageAccumulator,
            Core.ErrorHandler errorHandler,
            Core.ApplicationManagement applicationManagement,
            Core.IRC.IrcClient ircClient,
            Core.API.Twitch.IBotTokenValidator botTokenValidator,
            Core.API.Twitch.IBroadcasterTokenValidator broadcasterTokenValidator,
            Core.WebSub.WebSubHandler webSubHandler,
            Core.PubSub.PubSubClient pubSubClient,
            Core.Audio.IMicrophoneHandler microphoneHandler,
            Core.Audio.MidiKeyboardHandler midiKeyboardHandler)
        {
            this.communication = communication;
            this.messageAccumulator = messageAccumulator;
            this.errorHandler = errorHandler;
            this.applicationManagement = applicationManagement;
            this.ircClient = ircClient;
            this.botTokenValidator = botTokenValidator;
            this.broadcasterTokenValidator = broadcasterTokenValidator;
            this.webSubHandler = webSubHandler;
            this.pubSubClient = pubSubClient;
            this.microphoneHandler = microphoneHandler;
            this.midiKeyboardHandler = midiKeyboardHandler;
        }

        public async Task RunAsync()
        {
            try
            {
                communication.SendDebugMessage("*** Starting Up ***");

                microphoneHandler.Start();

                communication.SendDebugMessage("Connecting to IRC");

                await ircClient.Start();

                //Kick off Validator
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
            await webSubHandler.Subscribe();

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
                midiKeyboardHandler.Dispose();
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
                webSubHandler.Dispose();
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
