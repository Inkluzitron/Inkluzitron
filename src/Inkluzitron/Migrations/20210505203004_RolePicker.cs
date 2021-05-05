using Microsoft.EntityFrameworkCore.Migrations;

namespace Inkluzitron.Migrations
{
    public partial class RolePicker : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserRoleMessage",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    MessageId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    CanSelectMultiple = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleMessage", x => new { x.GuildId, x.ChannelId, x.MessageId });
                });

            migrationBuilder.CreateTable(
                name: "UserRoleMessageItem",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    MessageId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Mention = table.Column<string>(type: "TEXT", nullable: true),
                    Emote = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoleMessageItem", x => new { x.Id, x.GuildId, x.ChannelId, x.MessageId });
                    table.ForeignKey(
                        name: "FK_UserRoleMessageItem_UserRoleMessage_GuildId_ChannelId_MessageId",
                        columns: x => new { x.GuildId, x.ChannelId, x.MessageId },
                        principalTable: "UserRoleMessage",
                        principalColumns: new[] { "GuildId", "ChannelId", "MessageId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoleMessageItem_GuildId_ChannelId_MessageId",
                table: "UserRoleMessageItem",
                columns: new[] { "GuildId", "ChannelId", "MessageId" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRoleMessageItem");

            migrationBuilder.DropTable(
                name: "UserRoleMessage");
        }
    }
}
