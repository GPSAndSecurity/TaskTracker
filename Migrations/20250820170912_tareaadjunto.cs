using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Migrations
{
    /// <inheritdoc />
    public partial class tareaadjunto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ComentarioId",
                table: "TareaAdjuntos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TareaAdjuntos_ComentarioId",
                table: "TareaAdjuntos",
                column: "ComentarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_TareaAdjuntos_TareaComentarios_ComentarioId",
                table: "TareaAdjuntos",
                column: "ComentarioId",
                principalTable: "TareaComentarios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TareaAdjuntos_TareaComentarios_ComentarioId",
                table: "TareaAdjuntos");

            migrationBuilder.DropIndex(
                name: "IX_TareaAdjuntos_ComentarioId",
                table: "TareaAdjuntos");

            migrationBuilder.DropColumn(
                name: "ComentarioId",
                table: "TareaAdjuntos");
        }
    }
}
