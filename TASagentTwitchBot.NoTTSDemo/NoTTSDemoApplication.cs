using System;
using System.Threading.Tasks;

namespace TASagentTwitchBot.NoTTSDemo
{
    public class NoTTSDemoApplication
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

        public NoTTSDemoApplication(
            Core.Config.IBotConfigContainer botConfigContainer,
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

            BGC.Debug.ExceptionCallback += errorHandler.LogExternalException;

            if (string.IsNullOrEmpty(botConfigContainer.BotConfig.TwitchClientId) ||
                string.IsNullOrEmpty(botConfigContainer.BotConfig.BroadcasterId))
            {
                throw new Exception($"Bot not Configured.");
            }

            //Assign library log handlers
            BGC.Debug.LogCallback += communication.SendDebugMessage;
            BGC.Debug.LogWarningCallback += communication.SendWarningMessage;
            BGC.Debug.LogErrorCallback += communication.SendErrorMessage;
        }

        public async Task RunAsync()
        {
            try
            {
                communication.SendDebugMessage("*** Starting Up ***");
                communication.SendDebugMessage("Connecting to Twitch");

                if (!await botTokenValidator.TryToConnect())
                {
                    communication.SendErrorMessage("------------> URGENT <------------");
                    communication.SendErrorMessage("Please check bot credential process and try again.");
                    communication.SendErrorMessage("Unable to connect to Twitch");
                    communication.SendErrorMessage("Exiting bot application now...");
                    await Task.Delay(7500);
                    Environment.Exit(1);
                }

                if (!await broadcasterTokenValidator.TryToConnect())
                {
                    communication.SendErrorMessage("------------> URGENT <------------");
                    communication.SendErrorMessage("Please check broadcaster credential process and try again.");
                    communication.SendErrorMessage("Unable to connect to Twitch");
                    communication.SendErrorMessage("Exiting bot application now...");
                    await Task.Delay(7500);
                    Environment.Exit(1);
                }

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
