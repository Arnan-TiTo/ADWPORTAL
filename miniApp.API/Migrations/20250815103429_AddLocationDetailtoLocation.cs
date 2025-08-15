using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace miniApp.API.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationDetailtoLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Locations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Building",
                table: "Locations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractPerson",
                table: "Locations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractPhone",
                table: "Locations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "Locations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlaceName",
                table: "Locations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Postcode",
                table: "Locations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "Locations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "Building",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "ContractPerson",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "ContractPhone",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "District",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "PlaceName",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "Postcode",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "Locations");
        }
    }
}
