using Microsoft.EntityFrameworkCore;

namespace TASagentTwitchBot.SimpleDemo.Database;

//Create the database 
public class DatabaseContext : Core.Database.BaseDatabaseContext, Plugin.Quotes.IQuoteDatabaseContext
{
    public DbSet<SupplementalData> SupplementalData { get; set; } = null!;
    public DbSet<Plugin.Quotes.Quote> Quotes { get; set; } = null!;

    public async Task<SupplementalData> GetSupplementalDataAsync(Core.Database.User user)
    {
        SupplementalData? userSupplementalData = await SupplementalData.FirstOrDefaultAsync(x => x.UserId == user.UserId);

        if (userSupplementalData is null)
        {
            userSupplementalData = new SupplementalData()
            {
                UserId = user.UserId
            };

            SupplementalData.Add(userSupplementalData);
            await SaveChangesAsync();
        }

        return userSupplementalData;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={BGC.IO.DataManagement.PathForDataFile("Config", "data.sqlite")}");
    }
}

public class SupplementalData
{
    public int SupplementalDataId { get; set; }

    public int UserId { get; set; }
    public Core.Database.User User { get; set; } = null!;

    public int PointsSpent { get; set; }
    public DateTime? LastPointsSpentUpdate { get; set; }
}
