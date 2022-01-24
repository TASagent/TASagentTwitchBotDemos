using TASagentTwitchBot.Core;
using TASagentTwitchBot.Core.Config;

namespace TASagentTwitchBot.WebTTSOnly;

public class WebTTSOnlyView : Core.View.BasicView
{
    private readonly Core.Notifications.IActivityDispatcher activityDispatcher;

    public WebTTSOnlyView(
        BotConfiguration botConfig,
        ICommunication communication,
        ApplicationManagement applicationManagement,
        Core.Notifications.IActivityDispatcher activityDispatcher)
        : base(botConfig, communication, applicationManagement)
    {
        this.activityDispatcher = activityDispatcher;

        communication.SendDebugMessage("Press \"S\" to skip the current TTS.\n");
    }

    protected override void HandleKeys(in ConsoleKeyInfo input)
    {
        switch (input.Key)
        {
            case ConsoleKey.S:
                //Skip
                activityDispatcher.Skip();
                break;
        }
    }
}
