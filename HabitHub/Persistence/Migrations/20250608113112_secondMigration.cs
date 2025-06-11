using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class secondMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PercentageCompletion",
                table: "Habits");

            migrationBuilder.RenameColumn(
                name: "IsDone",
                table: "Habits",
                newName: "IsActive");

            migrationBuilder.CreateTable(
                name: "HabitProgress",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HabitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    PercentageCompletion = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HabitProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HabitProgress_Habits_HabitId",
                        column: x => x.HabitId,
                        principalTable: "Habits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HabitProgress_HabitId",
                table: "HabitProgress",
                column: "HabitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HabitProgress");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "Habits",
                newName: "IsDone");

            migrationBuilder.AddColumn<float>(
                name: "PercentageCompletion",
                table: "Habits",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }
    }
}
