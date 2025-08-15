using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace miniApp.API.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialOrderHd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Social",
                table: "OrderHd",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(@"
            ALTER TABLE [miniapp].[dbo].[OrderHd]
            ADD CONSTRAINT CK_OrderHd_Social_IsJson
            CHECK (Social IS NULL OR ISJSON(Social) = 1);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Social",
                table: "OrderHd");
            
            migrationBuilder.Sql(@"
            ALTER TABLE [miniapp].[dbo].[OrderHd]
            DROP CONSTRAINT CK_OrderHd_Social_IsJson;
");
        }
    }
}
