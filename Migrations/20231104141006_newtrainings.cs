using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Engineer_MVC.Migrations
{
    /// <inheritdoc />
    public partial class newtrainings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TrainingId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Training",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Duration = table.Column<float>(type: "real", nullable: false),
                    Price = table.Column<float>(type: "real", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NumberOfPeople = table.Column<int>(type: "int", nullable: false),
                    TreatmentId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Training", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Training_AspNetUsers_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Training_Treatment_TreatmentId",
                        column: x => x.TreatmentId,
                        principalTable: "Treatment",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TrainingId",
                table: "AspNetUsers",
                column: "TrainingId");

            migrationBuilder.CreateIndex(
                name: "IX_Training_EmployeeId",
                table: "Training",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Training_TreatmentId",
                table: "Training",
                column: "TreatmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Training_TrainingId",
                table: "AspNetUsers",
                column: "TrainingId",
                principalTable: "Training",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Training_TrainingId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Training");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_TrainingId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TrainingId",
                table: "AspNetUsers");
        }
    }
}
