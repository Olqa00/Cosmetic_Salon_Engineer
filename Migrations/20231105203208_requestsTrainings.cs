using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Engineer_MVC.Migrations
{
    /// <inheritdoc />
    public partial class requestsTrainings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCanceled",
                table: "Training");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Training");

            migrationBuilder.CreateTable(
                name: "CancellationRequest",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrainingId = table.Column<int>(type: "int", nullable: false),
                    IsCanceled = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CancellationRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CancellationRequest_Training_TrainingId",
                        column: x => x.TrainingId,
                        principalTable: "Training",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CancellationRequest_TrainingId",
                table: "CancellationRequest",
                column: "TrainingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CancellationRequest");

            migrationBuilder.AddColumn<bool>(
                name: "IsCanceled",
                table: "Training",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "Training",
                type: "int",
                nullable: true);
        }
    }
}
