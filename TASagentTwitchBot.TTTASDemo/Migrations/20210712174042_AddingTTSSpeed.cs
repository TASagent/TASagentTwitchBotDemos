using Microsoft.EntityFrameworkCore.Migrations;

namespace TASagentTwitchBot.TTTASDemo.Migrations
{
    public partial class AddingTTSSpeed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TTSSpeedPreference",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TTSSpeedPreference",
                table: "Users");
        }
    }
}
