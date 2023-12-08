using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Engineer_MVC.Migrations
{
    /// <inheritdoc />
    public partial class newtrainings2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Training_TrainingId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_TrainingId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TrainingId",
                table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "NumberOfPeople",
                table: "Training",
                newName: "UsersNumber");

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

            migrationBuilder.CreateTable(
                name: "TrainingUser",
                columns: table => new
                {
                    TrainingsReceiveId = table.Column<int>(type: "int", nullable: false),
                    UsersId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingUser", x => new { x.TrainingsReceiveId, x.UsersId });
                    table.ForeignKey(
                        name: "FK_TrainingUser_AspNetUsers_UsersId",
                        column: x => x.UsersId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TrainingUser_Training_TrainingsReceiveId",
                        column: x => x.TrainingsReceiveId,
                        principalTable: "Training",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingUser_UsersId",
                table: "TrainingUser",
                column: "UsersId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrainingUser");

            migrationBuilder.DropColumn(
                name: "IsCanceled",
                table: "Training");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Training");

            migrationBuilder.RenameColumn(
                name: "UsersNumber",
                table: "Training",
                newName: "NumberOfPeople");

            migrationBuilder.AddColumn<int>(
                name: "TrainingId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TrainingId",
                table: "AspNetUsers",
                column: "TrainingId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Training_TrainingId",
                table: "AspNetUsers",
                column: "TrainingId",
                principalTable: "Training",
                principalColumn: "Id");
        }
    }
}
