using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using TASagentTwitchBot.Core;

namespace TASagentTwitchBot.TTTASDemo.View;

public class TTTASBasicView : Core.View.BasicView
{
    private readonly Core.Notifications.IActivityDispatcher activityDispatcher;
    private readonly Plugin.TTTAS.ITTTASProvider tttasProvider;

    public TTTASBasicView(
        Core.Config.BotConfiguration botConfig,
        ICommunication communication,
        Plugin.TTTAS.ITTTASProvider tttasProvider,
        Core.Notifications.IActivityDispatcher activityDispatcher,
        ApplicationManagement applicationManagement)
        : base(
            botConfig: botConfig,
            communication: communication,
            applicationManagement: applicationManagement)
    {
        this.tttasProvider = tttasProvider;
        this.activityDispatcher = activityDispatcher;

        communication.SendDebugMessage("Press A to show current TTTAS prompts.");
        communication.SendDebugMessage("Press S to Start or Restart recording current TTTAS prompt.");
        communication.SendDebugMessage("Press D to End recording and submit current TTTAS prompt.");
        communication.SendDebugMessage("Press F to Hide TTTAS prompts.");
        communication.SendDebugMessage("Press Q to End and Skip the active TTTAS playback.\n");
    }

    protected override void SendPublicChatHandler(string message) { }
    protected override void SendWhisperHandler(string username, string message) { }
    protected override void ReceiveMessageHandler(Core.IRC.TwitchChatter chatter) { }

    protected override void HandleKeys(in ConsoleKeyInfo input)
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
