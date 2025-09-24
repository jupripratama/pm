using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Pm.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PermissionName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Group = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.PermissionId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RoleName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RolePermissionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.RolePermissionId);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "PermissionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Username = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FullName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "PermissionId", "CreatedAt", "Description", "Group", "PermissionName" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(407), "View all users", "User", "user.view-any" },
                    { 2, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(978), "View user detail", "User", "user.view" },
                    { 3, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(982), "Create new user", "User", "user.create" },
                    { 4, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(985), "Update user", "User", "user.update" },
                    { 5, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(992), "Delete user", "User", "user.delete" },
                    { 6, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(995), "View all roles", "Role", "role.view-any" },
                    { 7, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(998), "View role detail", "Role", "role.view" },
                    { 8, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(1002), "Create new role", "Role", "role.create" },
                    { 9, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(1014), "Update role", "Role", "role.update" },
                    { 10, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(1017), "Delete role", "Role", "role.delete" },
                    { 11, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(1020), "View permissions", "Permission", "permission.view" },
                    { 12, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(1023), "Edit permissions", "Permission", "permission.edit" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleId", "CreatedAt", "Description", "IsActive", "RoleName" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 9, 24, 6, 28, 14, 11, DateTimeKind.Utc).AddTicks(1068), "Full system access", true, "Super Admin" },
                    { 2, new DateTime(2025, 9, 24, 6, 28, 14, 11, DateTimeKind.Utc).AddTicks(1707), "Administrative access", true, "Admin" },
                    { 3, new DateTime(2025, 9, 24, 6, 28, 14, 11, DateTimeKind.Utc).AddTicks(1775), "Standard user access", true, "User" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "RolePermissionId", "CreatedAt", "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(4577), 1, 1 },
                    { 2, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(5140), 2, 1 },
                    { 3, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(5142), 3, 1 },
                    { 4, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(5144), 4, 1 },
                    { 5, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(5146), 5, 1 },
                    { 6, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(5365), 6, 1 },
                    { 7, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(5367), 7, 1 },
                    { 8, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(5369), 8, 1 },
                    { 9, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(5370), 9, 1 },
                    { 10, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(5374), 10, 1 },
                    { 11, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(5375), 11, 1 },
                    { 12, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(5377), 12, 1 },
                    { 13, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(5380), 1, 2 },
                    { 14, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(5382), 2, 2 },
                    { 15, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(5385), 3, 2 },
                    { 16, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(5387), 4, 2 },
                    { 17, new DateTime(2025, 9, 24, 6, 28, 14, 13, DateTimeKind.Utc).AddTicks(5920), 2, 3 }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserId", "CreatedAt", "Email", "FullName", "IsActive", "LastLogin", "PasswordHash", "RoleId", "UpdatedAt", "Username" },
                values: new object[] { 1, new DateTime(2025, 9, 24, 6, 28, 14, 705, DateTimeKind.Utc).AddTicks(4067), "admin@yourdomain.com", "System Administrator", true, null, "$2a$11$ZVyilLKcyieXPCf5bNKoCOatxdiMkzP/nDz/pilpBEFuV6qjpQdlq", 1, null, "admin" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_PermissionName",
                table: "Permissions",
                column: "PermissionName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_RoleName",
                table: "Roles",
                column: "RoleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
