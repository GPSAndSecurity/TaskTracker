using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddSubTarea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubTarea_Tareas_TareaId",
                table: "SubTarea");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubTarea",
                table: "SubTarea");

            migrationBuilder.RenameTable(
                name: "SubTarea",
                newName: "SubTareas");

            migrationBuilder.RenameIndex(
                name: "IX_SubTarea_TareaId",
                table: "SubTareas",
                newName: "IX_SubTareas_TareaId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubTareas",
                table: "SubTareas",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SubTareas_Tareas_TareaId",
                table: "SubTareas",
                column: "TareaId",
                principalTable: "Tareas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubTareas_Tareas_TareaId",
                table: "SubTareas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SubTareas",
                table: "SubTareas");

            migrationBuilder.RenameTable(
                name: "SubTareas",
                newName: "SubTarea");

            migrationBuilder.RenameIndex(
                name: "IX_SubTareas_TareaId",
                table: "SubTarea",
                newName: "IX_SubTarea_TareaId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SubTarea",
                table: "SubTarea",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SubTarea_Tareas_TareaId",
                table: "SubTarea",
                column: "TareaId",
                principalTable: "Tareas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
