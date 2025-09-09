using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddEmpresaIdToUbicacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "Ubicaciones",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Ubicaciones_EmpresaId",
                table: "Ubicaciones",
                column: "EmpresaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ubicaciones_Empresas_EmpresaId",
                table: "Ubicaciones",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ubicaciones_Empresas_EmpresaId",
                table: "Ubicaciones");

            migrationBuilder.DropIndex(
                name: "IX_Ubicaciones_EmpresaId",
                table: "Ubicaciones");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "Ubicaciones");
        }
    }
}
