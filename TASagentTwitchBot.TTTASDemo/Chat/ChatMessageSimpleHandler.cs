﻿namespace TASagentTwitchBot.TTTASDemo.Chat;

/// <summary>
/// ChatMessageHandler that omits the Bits notificaiton call
/// </summary>
public class ChatMessageSimpleHandler : Core.Chat.IChatMessageHandler
{
    private readonly Core.ICommunication communication;

    private readonly IServiceScopeFactory scopeFactory;

    public ChatMessageSimpleHandler(
        Core.ICommunication communication,
        IServiceScopeFactory scopeFactory)
    {
        this.communication = communication;
        this.scopeFactory = scopeFactory;
    }

    public virtual async void HandleChatMessage(Core.IRC.IRCMessage message)
    {
        if (message.ircCommand != Core.IRC.IRCCommand.PrivMsg && message.ircCommand != Core.IRC.IRCCommand.Whisper)
        {
            communication.SendDebugMessage($"Error: Passing forward non-chat message:\n    {message}");
            return;
        }

        Core.IRC.TwitchChatter? chatter = await Core.IRC.TwitchChatter.FromIRCMessage(message, communication, scopeFactory);

        if (chatter is null)
        {
            return;
        }

        communication.DispatchChatMessage(chatter);
    }
}
