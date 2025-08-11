using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace miniApp.API.Migrations
{
    /// <inheritdoc />
    public partial class AddQrLoginUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QrLogin",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QrLogin",
                table: "Users");
        }
    }
}
