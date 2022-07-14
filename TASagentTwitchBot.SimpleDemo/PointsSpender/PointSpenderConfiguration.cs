using System.Text.Json;

namespace TASagentTwitchBot.SimpleDemo.PointsSpender;

public class PointSpenderConfiguration
{
    private static string ConfigFilePath => BGC.IO.DataManagement.PathForDataFile("Config", "PointSpender.json");
    private static readonly object _lock = new object();

    public int Version { get; set; } = 1;
    public const int CURRENT_VERSION = 2;

    public bool Enabled { get; set; } = false;
    public string PointSpenderID { get; set; } = "";
    public string PointsCommand { get; set; } = "points";
    public string LeaderboardCommand { get; set; } = "leaderboard";

    public string RedemptionMessage { get; set; } = "$REDEEMER_NAME has now spent $REDEEMER_POINTS channel points, " +
        "putting them in $REDEEMER_PLACE place and making a grand total of $TOTAL_POINTS spent channel points.";

    public string LeaderboardMessage { get; set; } = "A total of $TOTAL_POINTS Channel Points have been spent. Spending leaders: $LEADERBOARD";

    public string PointsSelfMessage { get; set; } = "@$REQUESTER_NAME, you have spent $TARGET_POINTS channel points " +
        "of the cumulative $TOTAL_POINTS that have been spent on this redemption, " +
        "putting you in $TARGET_PLACE place on the leaderboard.";

    public string PointsSelfNoneMessage { get; set; } = "@$REQUESTER_NAME, you have never spent any channel points with the Spend Channel Points redemption.";

    public string PointsOtherMessage { get; set; } = "@$REQUESTER_NAME, it seems $TARGET_NAME has spent $TARGET_POINTS channel points " +
        "of the cumulative $TOTAL_POINTS that have been spent on this redemption, " +
        "putting them in $TARGET_PLACE place on the leaderboard.";

    public string PointsOtherNoneMessage { get; set; } = "@$REQUESTER_NAME, it seems $TARGET_NAME has never spent any channel points with the Spend Channel Points redemption.";

    public string PointsOtherUserNotFound { get; set; } = "@$REQUESTER_NAME, it seems $TARGET_NAME has never spent any channel points with the Spend Channel Points redemption.";

    public static PointSpenderConfiguration GetConfig(PointSpenderConfiguration? defaultConfig = null)
    {
        PointSpenderConfiguration config;
        if (File.Exists(ConfigFilePath))
        {
            //Load existing config
            config = JsonSerializer.Deserialize<PointSpenderConfiguration>(File.ReadAllText(ConfigFilePath))!;

            if (config.Version < CURRENT_VERSION)
            {
                config.Version = CURRENT_VERSION;
                config.Serialize();
            }
        }
        else
        {
            config = defaultConfig ?? new PointSpenderConfiguration() { Version = CURRENT_VERSION };
            config.Serialize();
        }

        return config;
    }

    public void Serialize()
    {
        lock (_lock)
        {
            File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(this));
        }
    }
}
