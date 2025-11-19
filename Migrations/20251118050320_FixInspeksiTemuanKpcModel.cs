using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pm.Migrations
{
    /// <inheritdoc />
    public partial class FixInspeksiTemuanKpcModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InspeksiTemuanKpcs_Users_CreatedByUserUserId",
                table: "InspeksiTemuanKpcs");

            migrationBuilder.DropForeignKey(
                name: "FK_InspeksiTemuanKpcs_Users_DeletedByUserUserId",
                table: "InspeksiTemuanKpcs");

            migrationBuilder.DropForeignKey(
                name: "FK_InspeksiTemuanKpcs_Users_UpdatedByUserUserId",
                table: "InspeksiTemuanKpcs");

            migrationBuilder.DropIndex(
                name: "IX_InspeksiTemuanKpcs_CreatedByUserUserId",
                table: "InspeksiTemuanKpcs");

            migrationBuilder.DropIndex(
                name: "IX_InspeksiTemuanKpcs_DeletedByUserUserId",
                table: "InspeksiTemuanKpcs");

            migrationBuilder.DropIndex(
                name: "IX_InspeksiTemuanKpcs_UpdatedByUserUserId",
                table: "InspeksiTemuanKpcs");

            migrationBuilder.DropColumn(
                name: "CreatedByUserUserId",
                table: "InspeksiTemuanKpcs");

            migrationBuilder.DropColumn(
                name: "DeletedByUserUserId",
                table: "InspeksiTemuanKpcs");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserUserId",
                table: "InspeksiTemuanKpcs");

            migrationBuilder.CreateIndex(
                name: "IX_InspeksiTemuanKpcs_CreatedBy",
                table: "InspeksiTemuanKpcs",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InspeksiTemuanKpcs_DeletedBy",
                table: "InspeksiTemuanKpcs",
                column: "DeletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InspeksiTemuanKpcs_UpdatedBy",
                table: "InspeksiTemuanKpcs",
                column: "UpdatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_InspeksiTemuanKpcs_Users_CreatedBy",
                table: "InspeksiTemuanKpcs",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InspeksiTemuanKpcs_Users_DeletedBy",
                table: "InspeksiTemuanKpcs",
                column: "DeletedBy",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_InspeksiTemuanKpcs_Users_UpdatedBy",
                table: "InspeksiTemuanKpcs",
                column: "UpdatedBy",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InspeksiTemuanKpcs_Users_CreatedBy",
                table: "InspeksiTemuanKpcs");

            migrationBuilder.DropForeignKey(
                name: "FK_InspeksiTemuanKpcs_Users_DeletedBy",
                table: "InspeksiTemuanKpcs");

            migrationBuilder.DropForeignKey(
                name: "FK_InspeksiTemuanKpcs_Users_UpdatedBy",
                table: "InspeksiTemuanKpcs");

            migrationBuilder.DropIndex(
                name: "IX_InspeksiTemuanKpcs_CreatedBy",
                table: "InspeksiTemuanKpcs");

            migrationBuilder.DropIndex(
                name: "IX_InspeksiTemuanKpcs_DeletedBy",
                table: "InspeksiTemuanKpcs");

            migrationBuilder.DropIndex(
                name: "IX_InspeksiTemuanKpcs_UpdatedBy",
                table: "InspeksiTemuanKpcs");

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserUserId",
                table: "InspeksiTemuanKpcs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserUserId",
                table: "InspeksiTemuanKpcs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserUserId",
                table: "InspeksiTemuanKpcs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InspeksiTemuanKpcs_CreatedByUserUserId",
                table: "InspeksiTemuanKpcs",
                column: "CreatedByUserUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InspeksiTemuanKpcs_DeletedByUserUserId",
                table: "InspeksiTemuanKpcs",
                column: "DeletedByUserUserId");

            migrationBuilder.CreateIndex(
                name: "IX_InspeksiTemuanKpcs_UpdatedByUserUserId",
                table: "InspeksiTemuanKpcs",
                column: "UpdatedByUserUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_InspeksiTemuanKpcs_Users_CreatedByUserUserId",
                table: "InspeksiTemuanKpcs",
                column: "CreatedByUserUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_InspeksiTemuanKpcs_Users_DeletedByUserUserId",
                table: "InspeksiTemuanKpcs",
                column: "DeletedByUserUserId",
                principalTable: "Users",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_InspeksiTemuanKpcs_Users_UpdatedByUserUserId",
                table: "InspeksiTemuanKpcs",
                column: "UpdatedByUserUserId",
                principalTable: "Users",
                principalColumn: "UserId");
        }
    }
}
