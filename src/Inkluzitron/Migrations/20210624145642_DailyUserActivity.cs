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
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Day = table.Column<string>(type: "TEXT", nullable: false),
                    Points = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    MessagesSent = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    ReactionsAdded = table.Column<long>(type: "INTEGER", nullable: false, defaultValue: 0L),
                    Timestamp = table.Column<byte[]>(type: "BLOB", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyUsersActivities", x => new { x.UserId, x.Day });
                    table.ForeignKey(
                        name: "FK_DailyUsersActivities_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            if (migrationBuilder.IsSqlite())
            {
                migrationBuilder.Sql(
                @"
                CREATE TRIGGER SetDailyUserActivityTimestampOnUpdate
                AFTER UPDATE ON DailyUsersActivities
                BEGIN
                    UPDATE DailyUsersActivities
                    SET Timestamp = randomblob(8)
                    WHERE rowid = NEW.rowid;
                END
            ");

                migrationBuilder.Sql(
                    @"
                CREATE TRIGGER SetDailyUserActivityTimestampOnInsert
                AFTER INSERT ON DailyUsersActivities
                BEGIN
                    UPDATE DailyUsersActivities
                    SET Timestamp = randomblob(8)
                    WHERE rowid = NEW.rowid;
                END
            ");
            }

            migrationBuilder.Sql($@"
                INSERT INTO DailyUsersActivities ('Day', 'Points', 'UserId')
                    SELECT '{DateTime.Now:yyyy-MM-dd}', Points, Id FROM Users");

            migrationBuilder.Sql("UPDATE Users SET Points=0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyUsersActivities");

            if (migrationBuilder.IsSqlite())
            {
                migrationBuilder.Sql("DROP TRIGGER SetDailyUserActivityTimestampOnUpdate");
                migrationBuilder.Sql("DROP TRIGGER SetDailyUserActivityTimestampOnInsert");
            }
        }
    }
}
