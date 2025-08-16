using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShiftCompliance.Api.Migrations
{
    /// <inheritdoc />
    public partial class initail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShiftLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Operator = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Shift = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TimestampUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCompliant = table.Column<bool>(type: "bit", nullable: false),
                    Score = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftLogs_Operator_TimestampUtc",
                table: "ShiftLogs",
                columns: new[] { "Operator", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftLogs_Shift_TimestampUtc",
                table: "ShiftLogs",
                columns: new[] { "Shift", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftLogs_TimestampUtc",
                table: "ShiftLogs",
                column: "TimestampUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShiftLogs");
        }
    }
}
