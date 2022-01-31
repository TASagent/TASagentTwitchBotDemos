using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TASagentTwitchBot.EmoteEffectsDemo.Migrations
{
    public partial class UpdatingQuotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Quotes");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    QuoteId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatorId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FakeNewsExplanation = table.Column<string>(type: "TEXT", nullable: true),
                    IsFakeNews = table.Column<bool>(type: "INTEGER", nullable: false),
                    QuoteText = table.Column<string>(type: "TEXT", nullable: false),
                    Speaker = table.Column<string>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_CreatorId",
                table: "Quotes",
                column: "CreatorId");
        }
    }
}
