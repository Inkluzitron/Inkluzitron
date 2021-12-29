using Inkluzitron.Data.Entities;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Inkluzitron.Migrations
{
    public partial class UpdateUnsetBirthdayYear : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            if (!migrationBuilder.IsSqlite())
                return;

            migrationBuilder.Sql(
                "UPDATE Users " +
                $"SET BirthdayDate = '{User.UnsetBirthdayYear}' || strftime('-%m-%d', BirthdayDate) || ' 00:00:00' " +
                $"WHERE BirthdayDate IS NOT NULL AND strftime('%Y', BirthdayDate)='{User.LegacyUnsetBirthdayYear}';"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
