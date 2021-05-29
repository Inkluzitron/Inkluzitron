using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Threading.Tasks;
using Inkluzitron.Enums;
using Inkluzitron.Extensions;
using Inkluzitron.Models.BdsmTestOrgApi;
using Inkluzitron.Models.Settings;
using Inkluzitron.Modules.BdsmTestOrg;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Inkluzitron.Migrations
{
    public partial class ReworkedQuizSchema : Migration
    {
        static private async Task<Result> GetBdsmResultAsync(string link, IConfiguration config)
        {
            var testResultMatch = BdsmModule.TestResultLinkRegex.Match(link);

            if (!testResultMatch.Success)
                throw new ArgumentException(null, nameof(link));

            var testid = testResultMatch.Groups[1].Value;

            var bdsmSettings = new BdsmTestOrgSettings(config);

            var requestData = new Dictionary<string, string>
            {
                { "uauth[uid]", bdsmSettings.ApiKey.Uid },
                { "uauth[salt]", bdsmSettings.ApiKey.Salt },
                { "uauth[authsig]", bdsmSettings.ApiKey.AuthSig },
                { "rauth[rid]", testid }
            };

            var response = await new HttpClient().PostAsync(
                "https://bdsmtest.org/ajax/getresult",
                new FormUrlEncodedContent(requestData));

            var responseData = await response.Content.ReadAsStringAsync();
            var testResult = JsonConvert.DeserializeObject<Result>(responseData);

            return testResult;
        }

        static private void UserUp(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Gender",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: "Unspecified");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Users",
                type: "TEXT",
                nullable: true);

            // Copy username cache from quiz
            migrationBuilder.Sql(@"
                UPDATE Users SET Name = (
                    SELECT SubmittedByName FROM QuizResults
                        WHERE SubmittedById = Users.Id)
                ");

            if (!migrationBuilder.IsSqlite()) return;

            var configuration = Program.BuildConfiguration(Environment.GetCommandLineArgs());
            using var connection = new SqliteConnection(
                Program.BuildConnectionString(configuration["DatabaseFilePath"]));

            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = @"SELECT SubmittedById, Link FROM QuizResults
                WHERE Discriminator IS 'BdsmTestOrgQuizResult'
                ORDER BY SubmittedAt ASC";

            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                try
                {
                    var link = reader["Link"].ToString();
                    var id = reader["SubmittedById"].ToString();
                    var task = GetBdsmResultAsync(link, configuration);
                    task.Wait();
                    var test = task.Result;

                    migrationBuilder.Sql($"UPDATE Users SET Gender='{test.Gender}' WHERE Id={id}");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        static private void BdsmUp(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BdsmTestOrgResults",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Link = table.Column<string>(type: "TEXT", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BdsmTestOrgResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BdsmTestOrgResults_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BdsmTestOrgItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Trait = table.Column<string>(type: "TEXT", nullable: false),
                    Score = table.Column<double>(type: "REAL", nullable: false),
                    ParentId = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BdsmTestOrgItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BdsmTestOrgItems_BdsmTestOrgResults_ParentId",
                        column: x => x.ParentId,
                        principalTable: "BdsmTestOrgResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BdsmTestOrgItems_ParentId",
                table: "BdsmTestOrgItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_BdsmTestOrgResults_Link",
                table: "BdsmTestOrgResults",
                column: "Link",
                unique: true);

            // Move data from the old quiz table
            migrationBuilder.Sql(@"
                DELETE FROM QuizResults WHERE Discriminator IS NOT 'BdsmTestOrgQuizResult';

                INSERT INTO BdsmTestOrgResults (Link, SubmittedAt, UserId)
                    SELECT Link, SubmittedAt, SubmittedById FROM QuizResults;

                INSERT INTO BdsmTestOrgItems (Trait, Score, ParentId)
                    SELECT QuizItems.Key, QuizItems.Value, BdsmTestOrgResults.Id FROM QuizItems
                        JOIN QuizResults ON QuizItems.ParentResultId = QuizResults.ResultId
                        JOIN BdsmTestOrgResults ON QuizResults.Link = BdsmTestOrgResults.Link;
                ");

            // Convert traits to Enum
            var traits = Enum.GetValues<BdsmTrait>();
            foreach (var trait in traits)
            {
                migrationBuilder.UpdateData(
                    "BdsmTestOrgItems",
                    "Trait", trait.GetDisplayName(),
                    "Trait", trait.ToString());
            }

        }

        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserRoleMessageItem_UserRoleMessage_GuildId_ChannelId_MessageId",
                table: "UserRoleMessageItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserRoleMessageItem",
                table: "UserRoleMessageItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserRoleMessage",
                table: "UserRoleMessage");

            migrationBuilder.RenameTable(
                name: "UserRoleMessageItem",
                newName: "RoleMenuMessageRoles");

            migrationBuilder.RenameTable(
                name: "UserRoleMessage",
                newName: "RoleMenuMessages");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "RoleMenuMessageRoles",
                newName: "RoleId");

            migrationBuilder.RenameIndex(
                name: "IX_UserRoleMessageItem_GuildId_ChannelId_MessageId",
                table: "RoleMenuMessageRoles",
                newName: "IX_RoleMenuMessageRoles_GuildId_ChannelId_MessageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoleMenuMessageRoles",
                table: "RoleMenuMessageRoles",
                columns: new[] { "RoleId", "GuildId", "ChannelId", "MessageId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_RoleMenuMessages",
                table: "RoleMenuMessages",
                columns: new[] { "GuildId", "ChannelId", "MessageId" });

            migrationBuilder.CreateTable(
                name: "RicePurityResults",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Score = table.Column<uint>(type: "INTEGER", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RicePurityResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RicePurityResults_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RicePurityResults_UserId",
                table: "RicePurityResults",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_RoleMenuMessageRoles_RoleMenuMessages_GuildId_ChannelId_MessageId",
                table: "RoleMenuMessageRoles",
                columns: new[] { "GuildId", "ChannelId", "MessageId" },
                principalTable: "RoleMenuMessages",
                principalColumns: new[] { "GuildId", "ChannelId", "MessageId" },
                onDelete: ReferentialAction.Cascade);

            BdsmUp(migrationBuilder);
            UserUp(migrationBuilder);

            migrationBuilder.DropTable(
                name: "QuizItems");

            migrationBuilder.DropTable(
                name: "QuizResults");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RoleMenuMessageRoles_RoleMenuMessages_GuildId_ChannelId_MessageId",
                table: "RoleMenuMessageRoles");

            migrationBuilder.DropTable(
                name: "BdsmTestOrgItems");

            migrationBuilder.DropTable(
                name: "RicePurityResults");

            migrationBuilder.DropTable(
                name: "BdsmTestOrgResults");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RoleMenuMessages",
                table: "RoleMenuMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RoleMenuMessageRoles",
                table: "RoleMenuMessageRoles");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "RoleMenuMessages",
                newName: "UserRoleMessage");

            migrationBuilder.RenameTable(
                name: "RoleMenuMessageRoles",
                newName: "UserRoleMessageItem");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "UserRoleMessageItem",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_RoleMenuMessageRoles_GuildId_ChannelId_MessageId",
                table: "UserRoleMessageItem",
                newName: "IX_UserRoleMessageItem_GuildId_ChannelId_MessageId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserRoleMessage",
                table: "UserRoleMessage",
                columns: new[] { "GuildId", "ChannelId", "MessageId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserRoleMessageItem",
                table: "UserRoleMessageItem",
                columns: new[] { "Id", "GuildId", "ChannelId", "MessageId" });

            migrationBuilder.CreateTable(
                name: "QuizResults",
                columns: table => new
                {
                    ResultId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Discriminator = table.Column<string>(type: "TEXT", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SubmittedById = table.Column<ulong>(type: "INTEGER", nullable: false),
                    SubmittedByName = table.Column<string>(type: "TEXT", nullable: true),
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
                    ItemId = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Discriminator = table.Column<string>(type: "TEXT", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: true),
                    ParentResultId = table.Column<long>(type: "INTEGER", nullable: true),
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
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuizItems_ParentResultId",
                table: "QuizItems",
                column: "ParentResultId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizResults_Link",
                table: "QuizResults",
                column: "Link",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserRoleMessageItem_UserRoleMessage_GuildId_ChannelId_MessageId",
                table: "UserRoleMessageItem",
                columns: new[] { "GuildId", "ChannelId", "MessageId" },
                principalTable: "UserRoleMessage",
                principalColumns: new[] { "GuildId", "ChannelId", "MessageId" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
