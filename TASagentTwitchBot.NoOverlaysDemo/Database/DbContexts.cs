using Microsoft.EntityFrameworkCore;

namespace TASagentTwitchBot.NoOverlaysDemo.Database;

public class DatabaseContext : Core.Database.BaseDatabaseContext, Plugin.Quotes.IQuoteDatabaseContext
{
    public DbSet<Plugin.Quotes.Quote> Quotes { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={BGC.IO.DataManagement.PathForDataFile("Config", "data.sqlite")}");
    }
}
