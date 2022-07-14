using TASagentTwitchBot.Core.Notifications;

namespace TASagentTwitchBot.EmoteEffectsDemo;

public class ActivityDispatcherStub : IActivityDispatcher
{
    public void QueueActivity(ActivityRequest activity, bool approved) { }

    public bool ReplayNotification(int index) => false;

    public void Skip() { }

    public void UpdateAllRequests(string userId, bool approved) { }

    public bool UpdatePendingRequest(int index, bool approved) => false;
}
