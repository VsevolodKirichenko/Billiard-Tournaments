using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tournaments.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenScoreConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Match_Score",
                table: "Matches");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Match_Score",
                table: "Matches",
                sql: "(\"HomeScore\" IS NULL AND \"AwayScore\" IS NULL) OR (\"HomeScore\" IS NOT NULL AND \"AwayScore\" IS NOT NULL AND \"HomeScore\" BETWEEN 0 AND 100 AND \"AwayScore\" BETWEEN 0 AND 100 AND \"HomeScore\" <> \"AwayScore\")");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Match_Score",
                table: "Matches");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Match_Score",
                table: "Matches",
                sql: "(\"HomeScore\" IS NULL AND \"AwayScore\" IS NULL) OR (\"HomeScore\" BETWEEN 0 AND 100 AND \"AwayScore\" BETWEEN 0 AND 100 AND \"HomeScore\" <> \"AwayScore\")");
        }
    }
}
