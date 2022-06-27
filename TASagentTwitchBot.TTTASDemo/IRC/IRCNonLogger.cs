namespace TASagentTwitchBot.TTTASDemo.IRC;

public class IRCNonLogger : Core.IRC.IIRCLogger
{
    public IRCNonLogger()
    {
    }

    public void WriteLine(string line)
    {
        //Ignores requests to log IRC Messages
    }
}
