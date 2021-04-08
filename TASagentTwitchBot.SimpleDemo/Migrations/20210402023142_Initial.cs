using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace TASagentTwitchBot.SimpleDemo.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomTextCommands",
                columns: table => new
                {
                    CustomTextCommandId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Command = table.Column<string>(type: "TEXT", nullable: true),
                    Text = table.Column<string>(type: "TEXT", nullable: true),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomTextCommands", x => x.CustomTextCommandId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TwitchUserName = table.Column<string>(type: "TEXT", nullable: true),
                    TwitchUserId = table.Column<string>(type: "TEXT", nullable: true),
                    FirstSeen = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FirstFollowed = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TTSVoicePreference = table.Column<int>(type: "INTEGER", nullable: false),
                    TTSPitchPreference = table.Column<int>(type: "INTEGER", nullable: false),
                    TTSEffectsChain = table.Column<string>(type: "TEXT", nullable: true),
                    LastSuccessfulTTS = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AuthorizationLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    QuoteId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    QuoteText = table.Column<string>(type: "TEXT", nullable: true),
                    Speaker = table.Column<string>(type: "TEXT", nullable: true),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatorId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsFakeNews = table.Column<bool>(type: "INTEGER", nullable: false),
                    FakeNewsExplanation = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.QuoteId);
                    table.ForeignKey(
                        name: "FK_Quotes_Users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SupplementalData",
                columns: table => new
                {
                    SupplementalDataId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    PointsSpent = table.Column<int>(type: "INTEGER", nullable: false),
                    LastPointsSpentUpdate = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupplementalData", x => x.SupplementalDataId);
                    table.ForeignKey(
                        name: "FK_SupplementalData_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_CreatorId",
                table: "Quotes",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplementalData_UserId",
                table: "SupplementalData",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomTextCommands");

            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropTable(
                name: "SupplementalData");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
