using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatGen.Saves.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModelsOnDisk",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelsOnDisk", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpawnedObjects",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ModelOnDiskId = table.Column<string>(type: "TEXT", nullable: false),
                    X = table.Column<float>(type: "REAL", nullable: false),
                    Y = table.Column<float>(type: "REAL", nullable: false),
                    Z = table.Column<float>(type: "REAL", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpawnedObjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpawnedObjects_ModelsOnDisk_ModelOnDiskId",
                        column: x => x.ModelOnDiskId,
                        principalTable: "ModelsOnDisk",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpawnedObjects_ModelOnDiskId",
                table: "SpawnedObjects",
                column: "ModelOnDiskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpawnedObjects");

            migrationBuilder.DropTable(
                name: "ModelsOnDisk");
        }
    }
}
