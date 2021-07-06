using System;
using Microsoft.EntityFrameworkCore;

namespace TASagentTwitchBot.TTTASDemo.Database
{
    public class DatabaseContext : Core.Database.BaseDatabaseContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            //Hack to force initialization to our data directory for EF Core Utilities
            if (!BGC.IO.DataManagement.Initialized)
            {
                BGC.IO.DataManagement.Initialize("TASagentBotDemo");
            }

            options.UseSqlite($"Data Source={BGC.IO.DataManagement.PathForDataFile("Config", "data.sqlite")}");
        }
    }
}
