using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Inkluzitron.Migrations
{
    public partial class VoteReplyRecords : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VoteReplyRecords",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    MessageId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ReplyId = table.Column<ulong>(type: "INTEGER", nullable: false),
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
                name: "VoteReplyRecords");
        }
    }
}
