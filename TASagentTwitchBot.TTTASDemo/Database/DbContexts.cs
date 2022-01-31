using System;
using Microsoft.EntityFrameworkCore;

namespace TASagentTwitchBot.TTTASDemo.Database;

public class DatabaseContext : Core.Database.BaseDatabaseContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={BGC.IO.DataManagement.PathForDataFile("Config", "data.sqlite")}");
    }
}
