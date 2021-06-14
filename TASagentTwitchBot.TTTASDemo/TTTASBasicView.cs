using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using TASagentTwitchBot.Core;

namespace TASagentTwitchBot.TTTASDemo.View
{
    public class TTTASBasicView : Core.View.IConsoleOutput, IDisposable
    {
        private readonly Core.Config.BotConfiguration botConfig;
        private readonly ICommunication communication;
        private readonly Core.Notifications.IActivityDispatcher activityDispatcher;
        private readonly Plugin.TTTAS.ITTTASProvider tttasProvider;
        private readonly ApplicationManagement applicationManagement;

        private readonly CancellationTokenSource generalTokenSource = new CancellationTokenSource();
        private readonly CountdownEvent readers = new CountdownEvent(1);

        private readonly Channel<ConsoleKeyInfo> consoleChannel;

        private bool disposedValue = false;

        public TTTASBasicView(
            Core.Config.BotConfiguration botConfig,
            ICommunication communication,
            Plugin.TTTAS.ITTTASProvider tttasProvider,
            Core.Notifications.IActivityDispatcher activityDispatcher,
            ApplicationManagement applicationManagement)
        {
            this.botConfig = botConfig;
            this.communication = communication;
            this.tttasProvider = tttasProvider;
            this.activityDispatcher = activityDispatcher;
            this.applicationManagement = applicationManagement;

            consoleChannel = Channel.CreateUnbounded<ConsoleKeyInfo>();

            LaunchListeners();

            communication.ReceivePendingNotificationHandlers += ReceivePendingNotification;
            communication.ReceiveEventHandlers += ReceiveEventHandler;
            //communication.ReceiveMessageLoggers += ReceiveMessageHandler;
            //communication.SendMessageHandlers += SendPublicChatHandler;
            //communication.SendWhisperHandlers += SendWhisperHandler;
            communication.DebugMessageHandlers += DebugMessageHandler;

            communication.SendDebugMessage("TTTASBasicView Connected.  Listening for Ctrl+Q to quit gracefully.\n");
            communication.SendDebugMessage("Press A to show current TTTAS prompts.");
            communication.SendDebugMessage("Press S to Start or Restart recording current TTTAS prompt.");
            communication.SendDebugMessage("Press D to End recording and submit current TTTAS prompt.");
            communication.SendDebugMessage("Press F to Hide TTTAS prompts.");
            communication.SendDebugMessage("Press Q to End and Skip the active TTTAS playback.\n");
        }

        private void ReceiveEventHandler(string message)
        {
            Console.WriteLine($"Event   {message}");
        }

        private void ReceivePendingNotification(int id, string message)
        {
            Console.WriteLine($"Notice  Pending Notification {id}: {message}");
        }

        private void DebugMessageHandler(string message, MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.Debug:
                    Console.WriteLine(message);
                    break;

                case MessageType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;

                case MessageType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;

                default:
                    throw new NotSupportedException($"Unexpected messageType: {messageType}");
            }
        }

        private void SendPublicChatHandler(string message)
        {
            Console.WriteLine($"Chat    {botConfig.BotName}: {message}");
        }

        private void SendWhisperHandler(string username, string message)
        {
            Console.WriteLine($"Chat    {botConfig.BotName} whispers {username}: {message}");
        }

        private void ReceiveMessageHandler(Core.IRC.TwitchChatter chatter)
        {
            Console.WriteLine($"Chat    {chatter.User.TwitchUserName}: {chatter.Message}");
        }

        public void LaunchListeners()
        {
            ReadKeysHandler();
            HandleKeysLoop();
        }

        private async Task<ConsoleKeyInfo> WaitForConsoleKeyInfo()
        {
            ConsoleKeyInfo keyInfo = default;
            try
            {
                await Task.Run(() => keyInfo = Console.ReadKey(true));
            }
            catch (Exception ex)
            {
                communication.SendErrorMessage($"BasicView Exception: {ex}");
            }

            return keyInfo;
        }

        private async void ReadKeysHandler()
        {
            try
            {
                readers.AddCount();

                while (true)
                {
                    ConsoleKeyInfo nextKey = await WaitForConsoleKeyInfo().WithCancellation(generalTokenSource.Token);

                    //Bail if we're trying to quit
                    if (generalTokenSource.IsCancellationRequested)
                    {
                        break;
                    }

                    await consoleChannel.Writer.WriteAsync(nextKey);
                }
            }
            catch (TaskCanceledException) { /* swallow */}
            catch (OperationCanceledException) { /* swallow */}
            catch (Exception ex)
            {
                //Log Error
                communication.SendErrorMessage($"BasicView Exception: {ex}");
            }
            finally
            {
                readers.Signal();
            }
        }


        private async void HandleKeysLoop()
        {
            while (true)
            {
                Console.CursorVisible = false;
                ConsoleKeyInfo input = await consoleChannel.Reader.ReadAsync();

                if (input.Key == ConsoleKey.Q && ((input.Modifiers & ConsoleModifiers.Control) != 0))
                {
                    applicationManagement.TriggerExit();
                }
                else
                {
                    switch (input.Key)
                    {
                        case ConsoleKey.A:
                            //Show Prompt
                            tttasProvider.ShowPrompt();
                            break;

                        case ConsoleKey.S:
                            //Start Record
                            tttasProvider.StartRecording();
                            break;

                        case ConsoleKey.D:
                            //End Record
                            tttasProvider.EndRecording();
                            break;

                        case ConsoleKey.F:
                            //Hide
                            tttasProvider.ClearPrompt();
                            break;

                        case ConsoleKey.Q:
                            //Skip
                            activityDispatcher.Skip();
                            break;
                    }
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    communication.ReceiveEventHandlers -= ReceiveEventHandler;
                    //communication.ReceiveMessageLoggers -= ReceiveMessageHandler;
                    //communication.SendMessageHandlers -= SendPublicChatHandler;
                    //communication.SendWhisperHandlers -= SendWhisperHandler;
                    communication.DebugMessageHandlers -= DebugMessageHandler;

                    generalTokenSource.Cancel();

                    readers.Signal();
                    readers.Wait();
                    readers.Dispose();

                    generalTokenSource.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
