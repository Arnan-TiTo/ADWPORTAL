using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace miniApp.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentFieldsToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PaymentSlip",
                table: "OrderHd",
                newName: "SlipImage");

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "OrderHd",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "OrderHd");

            migrationBuilder.RenameColumn(
                name: "SlipImage",
                table: "OrderHd",
                newName: "PaymentSlip");
        }
    }
}
