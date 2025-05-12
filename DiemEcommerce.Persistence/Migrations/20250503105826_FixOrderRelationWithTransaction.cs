using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiemEcommerce.Persistence.Migrations
{
    [ExcludeFromCodeCoverage]
    /// <inheritdoc />
    public partial class FixOrderRelationWithTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_OrdersId",
                table: "Transactions");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OrdersId",
                table: "Transactions",
                column: "OrdersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_OrdersId",
                table: "Transactions");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_OrdersId",
                table: "Transactions",
                column: "OrdersId",
                unique: true);
        }
    }
}
