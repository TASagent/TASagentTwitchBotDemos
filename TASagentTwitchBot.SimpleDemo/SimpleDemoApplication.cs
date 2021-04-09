using System;
using System.Threading.Tasks;

namespace TASagentTwitchBot.SimpleDemo
{
    public class SimpleDemoApplication
    {
        private readonly Core.API.Twitch.TokenValidator botTokenValidator;
        private readonly Core.API.Twitch.TokenValidator broadcasterTokenValidator;
        private readonly Core.Follows.FollowerWebSubClient webSubClient;
        private readonly Core.PubSub.PubSubClient pubSubClient;

        private readonly Core.ICommunication communication;
        private readonly Core.IRC.IrcClient ircClient;
        private readonly Core.ErrorHandler errorHandler;
        private readonly Core.IMessageAccumulator messageAccumulator;

        private readonly Core.Audio.IMicrophoneHandler microphoneHandler;
        private readonly Core.Audio.MidiKeyboardHandler midiKeyboardHandler;

        private readonly Core.ApplicationManagement applicationManagement;

        public SimpleDemoApplication(
            Core.Config.IBotConfigContainer botConfigContainer,
            Core.API.Twitch.HelixHelper helixHelper,
            Core.ErrorHandler errorHandler,
            Core.IRC.IrcClient ircClient,
            Core.Follows.FollowerWebSubClient webSubClient,
            Core.PubSub.PubSubClient pubSubClient,
            Core.IMessageAccumulator messageAccumulator,
            Core.Audio.IMicrophoneHandler microphoneHandler,
            Core.ICommunication communication,
            Core.Audio.MidiKeyboardHandler midiKeyboardHandler,
            Core.ApplicationManagement applicationManagement)
        {
            this.ircClient = ircClient;
            this.microphoneHandler = microphoneHandler;
            this.webSubClient = webSubClient;
            this.pubSubClient = pubSubClient;
            this.errorHandler = errorHandler;
            this.communication = communication;
            this.messageAccumulator = messageAccumulator;
            this.midiKeyboardHandler = midiKeyboardHandler;
            this.applicationManagement = applicationManagement;

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

            botTokenValidator = new Core.API.Twitch.TokenValidator(botConfigContainer, communication, true, helixHelper);
            broadcasterTokenValidator = new Core.API.Twitch.TokenValidator(botConfigContainer, communication, false, helixHelper);
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

                //Try up to 3 times to connect
                for (int i = 0; i < 3; i++)
                {
                    if (i > 0)
                    {
                        await Task.Delay(2000 * i);
                    }

                    try
                    {
                        //Attach sub listener
                        await webSubClient.Connect();

                        //No exception, break out of loop
                        break;
                    }
                    catch (Exception ex)
                    {
                        errorHandler.LogSystemException(ex);

                        if (i == 2)
                        {
                            throw new Exception("Unable to start Webhook after 3 attempts");
                        }
                    }
                }

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
                webSubClient.Dispose();
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
