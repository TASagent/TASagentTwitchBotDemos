using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TASagentTwitchBot.BasicMicController
{
    public class BasicMicApplication
    {
        private readonly Core.ICommunication communication;
        private readonly Core.ErrorHandler errorHandler;
        private readonly Core.ApplicationManagement applicationManagement;
        private readonly Core.IMessageAccumulator messageAccumulator;

        private readonly Core.Audio.IMicrophoneHandler microphoneHandler;

        private readonly Core.Audio.MidiKeyboardHandler midiKeyboardHandler;

        public BasicMicApplication(
            Core.ErrorHandler errorHandler,
            Core.ApplicationManagement applicationManagement,
            Core.IMessageAccumulator messageAccumulator,
            Core.Audio.IMicrophoneHandler microphoneHandler,
            Core.ICommunication communication,
            Core.Audio.MidiKeyboardHandler midiKeyboardHandler)
        {
            this.microphoneHandler = microphoneHandler;
            this.errorHandler = errorHandler;
            this.applicationManagement = applicationManagement;
            this.communication = communication;
            this.messageAccumulator = messageAccumulator;
            this.midiKeyboardHandler = midiKeyboardHandler;

            BGC.Debug.ExceptionCallback += errorHandler.LogExternalException;

            //Assign library log handlers
            BGC.Debug.LogCallback += communication.SendDebugMessage;
            BGC.Debug.LogWarningCallback += communication.SendWarningMessage;
            BGC.Debug.LogErrorCallback += communication.SendErrorMessage;
        }

        public async Task RunAsync()
        {
            try
            {
                communication.SendDebugMessage("*** Starting Up Basic Mic Application ***");

                microphoneHandler.Start();
            }
            catch (Exception ex)
            {
                errorHandler.LogFatalException(ex);
            }

            messageAccumulator.MonitorMessages();

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
        }
    }
}
