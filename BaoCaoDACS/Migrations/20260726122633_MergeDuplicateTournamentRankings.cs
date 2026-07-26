using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaoCaoDACS.Migrations
{
    /// <inheritdoc />
    public partial class MergeDuplicateTournamentRankings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TournamentRankings_UserId",
                table: "TournamentRankings");

            migrationBuilder.Sql(
                """
                ;WITH Ranked AS
                (
                    SELECT
                        Id,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY UserId, TournamentId
                            ORDER BY UpdatedAt DESC, Id DESC
                        ) AS DuplicateRank
                    FROM TournamentRankings
                )
                DELETE FROM Ranked
                WHERE DuplicateRank > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRankings_UserId_TournamentId",
                table: "TournamentRankings",
                columns: new[] { "UserId", "TournamentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TournamentRankings_UserId_TournamentId",
                table: "TournamentRankings");

            migrationBuilder.CreateIndex(
                name: "IX_TournamentRankings_UserId",
                table: "TournamentRankings",
                column: "UserId");
        }
    }
}
