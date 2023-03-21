using Microsoft.EntityFrameworkCore.Migrations;

namespace Inkluzitron.Migrations
{
    public partial class RemoveRoleMenu : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleMenuMessageRoles");

            migrationBuilder.DropTable(
                name: "RoleMenuMessages");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RoleMenuMessages",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    MessageId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    CanSelectMultiple = table.Column<bool>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleMenuMessages", x => new { x.GuildId, x.ChannelId, x.MessageId });
                });

            migrationBuilder.CreateTable(
                name: "RoleMenuMessageRoles",
                columns: table => new
                {
                    RoleId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    MessageId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Emote = table.Column<string>(type: "TEXT", nullable: true),
                    Mention = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleMenuMessageRoles", x => new { x.RoleId, x.GuildId, x.ChannelId, x.MessageId });
                    table.ForeignKey(
                        name: "FK_RoleMenuMessageRoles_RoleMenuMessages_GuildId_ChannelId_MessageId",
                        columns: x => new { x.GuildId, x.ChannelId, x.MessageId },
                        principalTable: "RoleMenuMessages",
                        principalColumns: new[] { "GuildId", "ChannelId", "MessageId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RoleMenuMessageRoles_GuildId_ChannelId_MessageId",
                table: "RoleMenuMessageRoles",
                columns: new[] { "GuildId", "ChannelId", "MessageId" });
        }
    }
}
