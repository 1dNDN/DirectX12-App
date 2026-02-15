using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatGen.Saves.Migrations
{
    /// <inheritdoc />
    public partial class AddRollAndScale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Pitch",
                table: "SpawnedObjects",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Roll",
                table: "SpawnedObjects",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Scale",
                table: "SpawnedObjects",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "Yaw",
                table: "SpawnedObjects",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pitch",
                table: "SpawnedObjects");

            migrationBuilder.DropColumn(
                name: "Roll",
                table: "SpawnedObjects");

            migrationBuilder.DropColumn(
                name: "Scale",
                table: "SpawnedObjects");

            migrationBuilder.DropColumn(
                name: "Yaw",
                table: "SpawnedObjects");
        }
    }
}
