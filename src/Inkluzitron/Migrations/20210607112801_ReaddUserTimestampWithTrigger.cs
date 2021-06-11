using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Inkluzitron.Migrations
{
    public partial class ReaddUserTimestampWithTrigger : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Timestamp",
                table: "Users",
                type: "BLOB",
                rowVersion: true,
                nullable: true);

            if (migrationBuilder.IsSqlite()) {
                migrationBuilder.Sql(
                 @"
                    CREATE TRIGGER SetUserTimestampOnUpdate
                    AFTER UPDATE ON Users
                    BEGIN
                        UPDATE Users
                        SET Timestamp = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");

                migrationBuilder.Sql(
                    @"
                    CREATE TRIGGER SetUserTimestampOnInsert
                    AFTER INSERT ON Users
                    BEGIN
                        UPDATE Users
                        SET Timestamp = randomblob(8)
                        WHERE rowid = NEW.rowid;
                    END
                ");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "Users");

            if (migrationBuilder.IsSqlite())
            {
                migrationBuilder.Sql("DROP TRIGGER SetUserTimestampOnUpdate");
                migrationBuilder.Sql("DROP TRIGGER SetUserTimestampOnInsert");
            }
        }
    }
}
