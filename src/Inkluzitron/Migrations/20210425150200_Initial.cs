using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Inkluzitron.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuizResults",
                columns: table => new
                {
                    ResultId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    SubmittedById = table.Column<ulong>(type: "INTEGER", nullable: false),
                    SubmittedByName = table.Column<string>(type: "TEXT", nullable: true),
                    Discriminator = table.Column<string>(type: "TEXT", nullable: false),
                    Link = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizResults", x => x.ResultId);
                });

            migrationBuilder.CreateTable(
                name: "QuizItems",
                columns: table => new
                {
                    ItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentResultId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Key = table.Column<string>(type: "TEXT", nullable: true),
                    Discriminator = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizItems", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_QuizItems_QuizResults_ParentResultId",
                        column: x => x.ParentResultId,
                        principalTable: "QuizResults",
                        principalColumn: "ResultId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuizItems_ParentResultId",
                table: "QuizItems",
                column: "ParentResultId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuizItems");

            migrationBuilder.DropTable(
                name: "QuizResults");
        }
    }
}
