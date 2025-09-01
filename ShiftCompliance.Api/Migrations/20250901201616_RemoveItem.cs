using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftCompliance.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedUtc",
                table: "ShiftItemBudgets",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedUtc",
                table: "ShiftItemBudgets",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedUtc",
                table: "ShiftItemBudgets");

            migrationBuilder.DropColumn(
                name: "UpdatedUtc",
                table: "ShiftItemBudgets");

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
    }
}
