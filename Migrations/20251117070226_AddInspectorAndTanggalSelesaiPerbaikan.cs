using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations
{
    /// <inheritdoc />
    public partial class AddInspectorAndTanggalSelesaiPerbaikan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Inspector",
                table: "InspeksiTemuanKpcs",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "TanggalSelesaiPerbaikan",
                table: "InspeksiTemuanKpcs",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Inspector",
                table: "InspeksiTemuanKpcs");

            migrationBuilder.DropColumn(
                name: "TanggalSelesaiPerbaikan",
                table: "InspeksiTemuanKpcs");
        }
    }
}
