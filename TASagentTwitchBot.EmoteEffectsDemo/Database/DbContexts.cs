using Microsoft.EntityFrameworkCore;

namespace TASagentTwitchBot.EmoteEffectsDemo.Database;

public class DatabaseContext : Core.Database.BaseDatabaseContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={BGC.IO.DataManagement.PathForDataFile("Config", "data.sqlite")}");
    }
}
