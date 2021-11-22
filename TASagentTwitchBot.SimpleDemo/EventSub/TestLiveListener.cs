namespace TASagentTwitchBot.SimpleDemo.EventSub;

public class TestLiveListener : Core.EventSub.IStreamLiveListener
{
    private readonly Core.ICommunication communication;

    public TestLiveListener(
        Core.ICommunication communication)
    {
        this.communication = communication;
    }

    public void NotifyLiveStatus(bool isLive)
    {
        communication.SendDebugMessage($"Channel is now {(isLive ? "Live" : "Not Live")}");
    }
}
