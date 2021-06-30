using Microsoft.EntityFrameworkCore.Migrations;

namespace Inkluzitron.Migrations
{
    public partial class UsersPointsRemove : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (!migrationBuilder.IsSqlite())
            {
                migrationBuilder.DropColumn("Points", "Users");
                return;
            }

            migrationBuilder.Sql(@"
                COMMIT;
                PRAGMA foreign_keys=OFF;

                BEGIN TRANSACTION;
                    CREATE TABLE new_Users (
                        Id INTEGER NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
                        CommandConsents INTEGER NOT NULL,
                        Gender TEXT NOT NULL,
                        LastMessagePointsIncrement TEXT NULL,
                        LastReactionPointsIncrement TEXT NULL,
                        Name TEXT NULL,
                        Timestamp BLOB NULL,
                        KisLastCheck TEXT NULL,
                        KisNickname TEXT NULL);

                    INSERT INTO new_Users SELECT
                        Id, CommandConsents, Gender, LastMessagePointsIncrement, LastReactionPointsIncrement,
                        Name, Timestamp, KisLastCheck, KisNickname
                        FROM Users;

                    DROP TABLE Users;

                    ALTER TABLE new_Users RENAME TO Users;

                    CREATE UNIQUE INDEX IX_Users_KisNickname ON Users(KisNickname);

                    CREATE TRIGGER SetUserTimestampOnInsert
                        AFTER INSERT ON Users
                        BEGIN
                            UPDATE Users
                            SET Timestamp = randomblob(8)
                            WHERE rowid = NEW.rowid;
                        END;

                    CREATE TRIGGER SetUserTimestampOnUpdate
                        AFTER UPDATE ON Users
                        BEGIN
                            UPDATE Users
                            SET Timestamp = randomblob(8)
                            WHERE rowid = NEW.rowid;
                        END;
                COMMIT;
                
                PRAGMA foreign_keys=ON;
                BEGIN TRANSACTION;");

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Points",
                table: "Users",
                type: "INTEGER",
                nullable: false);
        }
    }
}
