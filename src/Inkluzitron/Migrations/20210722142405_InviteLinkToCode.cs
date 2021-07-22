using Microsoft.EntityFrameworkCore.Migrations;

namespace Inkluzitron.Migrations
{
    public partial class InviteLinkToCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InviteLink",
                table: "Invites",
                newName: "InviteCode");

            migrationBuilder.RenameIndex(
                name: "IX_Invites_InviteLink",
                table: "Invites",
                newName: "IX_Invites_InviteCode");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InviteCode",
                table: "Invites",
                newName: "InviteLink");

            migrationBuilder.RenameIndex(
                name: "IX_Invites_InviteCode",
                table: "Invites",
                newName: "IX_Invites_InviteLink");
        }
    }
}
