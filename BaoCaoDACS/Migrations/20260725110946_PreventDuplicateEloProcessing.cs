using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaoCaoDACS.Migrations
{
    /// <inheritdoc />
    public partial class PreventDuplicateEloProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EloProcessed",
                table: "match",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE m
                SET m.EloProcessed = 1
                FROM [match] AS m
                WHERE (SELECT COUNT(*) FROM socre AS s WHERE s.MatchId = m.MatchId) = 2
                  AND EXISTS (SELECT 1 FROM socre AS s WHERE s.MatchId = m.MatchId AND s.Kq = 1)
                  AND EXISTS (SELECT 1 FROM socre AS s WHERE s.MatchId = m.MatchId AND s.Kq = 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EloProcessed",
                table: "match");
        }
    }
}
