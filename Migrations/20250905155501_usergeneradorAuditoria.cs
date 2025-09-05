using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Migrations
{
    /// <inheritdoc />
    public partial class usergeneradorAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UsuarioGeneradorId",
                table: "Auditorias",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Auditorias_UsuarioGeneradorId",
                table: "Auditorias",
                column: "UsuarioGeneradorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Auditorias_Usuarios_UsuarioGeneradorId",
                table: "Auditorias",
                column: "UsuarioGeneradorId",
                principalTable: "Usuarios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Auditorias_Usuarios_UsuarioGeneradorId",
                table: "Auditorias");

            migrationBuilder.DropIndex(
                name: "IX_Auditorias_UsuarioGeneradorId",
                table: "Auditorias");

            migrationBuilder.DropColumn(
                name: "UsuarioGeneradorId",
                table: "Auditorias");
        }
    }
}
