using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiemEcommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Feedback_Transactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverImages",
                table: "Matches");

            migrationBuilder.AddColumn<double>(
                name: "Balance",
                table: "User",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "MatchMedias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchesId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchMedias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchMedias_Matches_MatchesId",
                        column: x => x.MatchesId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchMedias_MatchesId",
                table: "MatchMedias",
                column: "MatchesId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MatchMedias");

            migrationBuilder.DropColumn(
                name: "Balance",
                table: "User");

            migrationBuilder.AddColumn<string[]>(
                name: "CoverImages",
                table: "Matches",
                type: "text[]",
                nullable: false,
                defaultValue: new string[0]);
        }
    }
}
