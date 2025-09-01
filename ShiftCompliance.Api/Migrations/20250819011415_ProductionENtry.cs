using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftCompliance.Api.Migrations
{
    /// <inheritdoc />
    public partial class ProductionENtry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    StandardCost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductionEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    No = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Shift = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ShiftSupervisor = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PostingDateUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsCompliant = table.Column<bool>(type: "bit", nullable: true),
                    ComplianceScore = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductionEntryLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductionEntryId = table.Column<int>(type: "int", nullable: false),
                    LineNo = table.Column<int>(type: "int", nullable: false),
                    ItemNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    DowntimeMinutes = table.Column<int>(type: "int", nullable: false),
                    OvertimeHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SafetyIncidents = table.Column<int>(type: "int", nullable: false),
                    Remark = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionEntryLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionEntryLines_ProductionEntries_ProductionEntryId",
                        column: x => x.ProductionEntryId,
                        principalTable: "ProductionEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_ItemNo",
                table: "Items",
                column: "ItemNo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionEntries_No",
                table: "ProductionEntries",
                column: "No",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionEntryLines_ProductionEntryId_LineNo",
                table: "ProductionEntryLines",
                columns: new[] { "ProductionEntryId", "LineNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "ProductionEntryLines");

            migrationBuilder.DropTable(
                name: "ProductionEntries");
        }
    }
}
