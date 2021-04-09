using System;
using System.Threading.Tasks;

namespace TASagentTwitchBot.FilesTest
{
    public class FilesTestApplication
    {
        private readonly Core.ICommunication communication;
        private readonly Core.ErrorHandler errorHandler;
        private readonly Core.ApplicationManagement applicationManagement;
        private readonly Core.IMessageAccumulator messageAccumulator;

        public FilesTestApplication(
            Core.ErrorHandler errorHandler,
            Core.ApplicationManagement applicationManagement,
            Core.IMessageAccumulator messageAccumulator,
            Core.ICommunication communication)
        {
            this.errorHandler = errorHandler;
            this.applicationManagement = applicationManagement;
            this.communication = communication;
            this.messageAccumulator = messageAccumulator;

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
                communication.SendDebugMessage("Files Test Application");
            }
            catch (Exception ex)
            {
                errorHandler.LogFatalException(ex);
            }

            messageAccumulator.MonitorMessages();


            communication.SendDebugMessage("*** Connected ***");
            communication.SendDebugMessage("Now check out http://localhost:5000/");

            try
            {
                await applicationManagement.WaitForEndAsync();
            }
            catch (Exception ex)
            {
                errorHandler.LogSystemException(ex);
            }
        }
    }
}
