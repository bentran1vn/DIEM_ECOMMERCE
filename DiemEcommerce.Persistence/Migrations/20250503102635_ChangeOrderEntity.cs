using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiemEcommerce.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ChangeOrderEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
