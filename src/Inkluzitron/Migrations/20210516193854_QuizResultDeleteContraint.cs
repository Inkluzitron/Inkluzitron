using Microsoft.EntityFrameworkCore.Migrations;

namespace Inkluzitron.Migrations
{
    public partial class QuizResultDeleteContraint : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuizItems_QuizResults_ParentResultId",
                table: "QuizItems");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizItems_QuizResults_ParentResultId",
                table: "QuizItems",
                column: "ParentResultId",
                principalTable: "QuizResults",
                principalColumn: "ResultId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuizItems_QuizResults_ParentResultId",
                table: "QuizItems");

            migrationBuilder.AddForeignKey(
                name: "FK_QuizItems_QuizResults_ParentResultId",
                table: "QuizItems",
                column: "ParentResultId",
                principalTable: "QuizResults",
                principalColumn: "ResultId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
