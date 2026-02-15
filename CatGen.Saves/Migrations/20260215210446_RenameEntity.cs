using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatGen.Saves.Migrations
{
    /// <inheritdoc />
    public partial class RenameEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpawnedObjects");

            migrationBuilder.CreateTable(
                name: "SpawnedEntities",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ModelOnDiskId = table.Column<string>(type: "TEXT", nullable: false),
                    X = table.Column<float>(type: "REAL", nullable: false),
                    Y = table.Column<float>(type: "REAL", nullable: false),
                    Z = table.Column<float>(type: "REAL", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Scale = table.Column<float>(type: "REAL", nullable: false),
                    Yaw = table.Column<float>(type: "REAL", nullable: false),
                    Pitch = table.Column<float>(type: "REAL", nullable: false),
                    Roll = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpawnedEntities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpawnedEntities_ModelsOnDisk_ModelOnDiskId",
                        column: x => x.ModelOnDiskId,
                        principalTable: "ModelsOnDisk",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpawnedEntities_ModelOnDiskId",
                table: "SpawnedEntities",
                column: "ModelOnDiskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpawnedEntities");

            migrationBuilder.CreateTable(
                name: "SpawnedObjects",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    ModelOnDiskId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Pitch = table.Column<float>(type: "REAL", nullable: false),
                    Roll = table.Column<float>(type: "REAL", nullable: false),
                    Scale = table.Column<float>(type: "REAL", nullable: false),
                    X = table.Column<float>(type: "REAL", nullable: false),
                    Y = table.Column<float>(type: "REAL", nullable: false),
                    Yaw = table.Column<float>(type: "REAL", nullable: false),
                    Z = table.Column<float>(type: "REAL", nullable: false)
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
    }
}
