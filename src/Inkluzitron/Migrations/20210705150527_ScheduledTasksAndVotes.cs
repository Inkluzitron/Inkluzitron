using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Inkluzitron.Migrations
{
    public partial class ScheduledTasksAndVotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduledTasks",
                columns: table => new
                {
                    ScheduledTaskId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Discriminator = table.Column<string>(type: "TEXT", nullable: false),
                    Tag = table.Column<string>(type: "TEXT", nullable: true),
                    Data = table.Column<string>(type: "TEXT", nullable: true),
                    FailCount = table.Column<int>(type: "INTEGER", nullable: false),
                    When = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledTasks", x => x.ScheduledTaskId);
                });

            migrationBuilder.CreateTable(
                name: "VoteReplyRecords",
                columns: table => new
                {
                    GuildId = table.Column<string>(type: "TEXT", nullable: false),
                    ChannelId = table.Column<string>(type: "TEXT", nullable: false),
                    MessageId = table.Column<string>(type: "TEXT", nullable: false),
                    ReplyId = table.Column<string>(type: "TEXT", nullable: false),
                    RecordCreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoteReplyRecords", x => new { x.GuildId, x.ChannelId, x.MessageId });
                    table.UniqueConstraint("AK_VoteReplyRecords_GuildId_ChannelId_ReplyId", x => new { x.GuildId, x.ChannelId, x.ReplyId });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledTasks");

            migrationBuilder.DropTable(
                name: "VoteReplyRecords");
        }
    }
}
