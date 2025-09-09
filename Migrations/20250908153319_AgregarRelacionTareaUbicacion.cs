using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Migrations
{
    /// <inheritdoc />
    public partial class AgregarRelacionTareaUbicacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ubicacion",
                table: "Tareas");

            migrationBuilder.AddColumn<int>(
                name: "UbicacionId",
                table: "Tareas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Ubicaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Latitud = table.Column<double>(type: "double", nullable: false),
                    Longitud = table.Column<double>(type: "double", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ubicaciones", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Tareas_UbicacionId",
                table: "Tareas",
                column: "UbicacionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tareas_Ubicaciones_UbicacionId",
                table: "Tareas",
                column: "UbicacionId",
                principalTable: "Ubicaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tareas_Ubicaciones_UbicacionId",
                table: "Tareas");

            migrationBuilder.DropTable(
                name: "Ubicaciones");

            migrationBuilder.DropIndex(
                name: "IX_Tareas_UbicacionId",
                table: "Tareas");

            migrationBuilder.DropColumn(
                name: "UbicacionId",
                table: "Tareas");

            migrationBuilder.AddColumn<string>(
                name: "Ubicacion",
                table: "Tareas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
