using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftCompliance.Api.Migrations
{
    /// <inheritdoc />
    public partial class UndoItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ItemId",
                table: "ShiftItemBudgets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShiftItemBudgets_ItemId",
                table: "ShiftItemBudgets",
                column: "ItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShiftItemBudgets_Items_ItemId",
                table: "ShiftItemBudgets",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShiftItemBudgets_Items_ItemId",
                table: "ShiftItemBudgets");

            migrationBuilder.DropIndex(
                name: "IX_ShiftItemBudgets_ItemId",
                table: "ShiftItemBudgets");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "ShiftItemBudgets");
        }
    }
}
