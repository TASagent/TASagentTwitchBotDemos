using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TASagentTwitchBot.SimpleDemo.Migrations
{
    public partial class RemovingTempVoiceField : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OldTTSVoicePreference",
                table: "Users");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OldTTSVoicePreference",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
