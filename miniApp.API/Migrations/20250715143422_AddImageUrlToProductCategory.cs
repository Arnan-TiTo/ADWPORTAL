using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace miniApp.API.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlToProductCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "ProductCategories",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "ProductCategories");
        }
    }
}
