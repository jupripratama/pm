using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations
{
    /// <inheritdoc />
    public partial class Callrecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CallRecords",
                columns: table => new
                {
                    CallRecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CallDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CallTime = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    CallCloseReason = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallRecords", x => x.CallRecordId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CallSummaries",
                columns: table => new
                {
                    CallSummaryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SummaryDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    HourGroup = table.Column<int>(type: "int", nullable: false),
                    TotalQty = table.Column<int>(type: "int", nullable: false),
                    TEBusyCount = table.Column<int>(type: "int", nullable: false),
                    SysBusyCount = table.Column<int>(type: "int", nullable: false),
                    OthersCount = table.Column<int>(type: "int", nullable: false),
                    TEBusyPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SysBusyPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    OthersPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallSummaries", x => x.CallSummaryId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CallRecord_CloseReason",
                table: "CallRecords",
                column: "CallCloseReason");

            migrationBuilder.CreateIndex(
                name: "IX_CallRecord_Date",
                table: "CallRecords",
                column: "CallDate");

            migrationBuilder.CreateIndex(
                name: "IX_CallRecord_HourQuery",
                table: "CallRecords",
                columns: new[] { "CallDate", "CallTime" });

            migrationBuilder.CreateIndex(
                name: "IX_CallSummary_DateHour",
                table: "CallSummaries",
                columns: new[] { "SummaryDate", "HourGroup" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CallRecords");

            migrationBuilder.DropTable(
                name: "CallSummaries");
        }
    }
}
