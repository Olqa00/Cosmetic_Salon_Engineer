using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Engineer_MVC.Migrations
{
    /// <inheritdoc />
    public partial class ImagesAgain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Post",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Post");
        }
    }
}
