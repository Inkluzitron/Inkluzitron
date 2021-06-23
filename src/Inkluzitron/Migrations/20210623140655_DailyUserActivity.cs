using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Inkluzitron.Migrations
{
    public partial class DailyUserActivity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DailyUsersActivities",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Day = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Points = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    MessagesSent = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    ReactionsAdded = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyUsersActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyUsersActivities_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyUsersActivities_UserId",
                table: "DailyUsersActivities",
                column: "UserId");

            var now = DateTime.Now.Date.ToString("yyyy-MM-dd HH:mm:ss");

            migrationBuilder.Sql($@"
                INSERT INTO DailyUsersActivities ('Day', 'Points', 'UserId')
                    SELECT '{now}', Points, Id FROM Users");

            migrationBuilder.DropColumn(
                name: "Points",
                table: "Users");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyUsersActivities");

            migrationBuilder.AddColumn<long>(
                name: "Points",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
