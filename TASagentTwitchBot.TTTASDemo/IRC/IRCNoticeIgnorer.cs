using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TASagentTwitchBot.TTTASDemo.IRC
{
    public class IRCNoticeIgnorer : Core.IRC.INoticeHandler
    {

        public IRCNoticeIgnorer()
        {

        }

        public void HandleIRCNotice(Core.IRC.IRCMessage message)
        {
            //Do Nothing - This class is meant to ignore IRC Notices
        }

    }
}
