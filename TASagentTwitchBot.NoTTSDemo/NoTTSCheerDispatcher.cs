namespace TASagentTwitchBot.NoTTSDemo;

public class NoTTSCheerDispatcher
{
    private readonly Core.Notifications.ICheerHandler cheerHandler;

    public NoTTSCheerDispatcher(
        Core.ICommunication communication,
        Core.Notifications.ICheerHandler cheerHandler)
    {
        this.cheerHandler = cheerHandler;

        communication.ReceiveMessageHandlers += CheerMessageHandler;
    }

    private void CheerMessageHandler(Core.IRC.TwitchChatter chatter)
    {
        if (chatter.Bits != 0)
        {
            cheerHandler.HandleCheer(chatter.User, chatter.Message, chatter.Bits, false, true);
        }
    }
}
