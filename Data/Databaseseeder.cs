using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.Models;

namespace Pm.Helper
{
    public static class DatabaseSeeder
    {
        /// <summary>
        /// Seed initial data untuk Roles, Permissions, RolePermissions, dan Super Admin
        /// Panggil di Program.cs setelah EnsureCreated()
        /// </summary>
        public static async Task SeedInitialDataAsync(this AppDbContext context, ILogger logger)
        {
            try
            {
                logger.LogInformation("🌱 Starting database seeding...");

                // 1. Seed Roles
                if (!await context.Roles.AnyAsync())
                {
                    logger.LogInformation("Seeding Roles...");
                    var roles = new List<Role>
                    {
                        new Role { RoleId = 1, RoleName = "Super Admin", Description = "Full system access with all permissions", IsActive = true, CreatedAt = DateTime.UtcNow },
                        new Role { RoleId = 2, RoleName = "Admin", Description = "Administrative access with limited permissions", IsActive = true, CreatedAt = DateTime.UtcNow },
                        new Role { RoleId = 3, RoleName = "User", Description = "Standard user access", IsActive = true, CreatedAt = DateTime.UtcNow }
                    };
                    await context.Roles.AddRangeAsync(roles);
                    await context.SaveChangesAsync();
                    logger.LogInformation("✅ Roles seeded successfully");
                }

                // 2. Seed Permissions
                if (!await context.Permissions.AnyAsync())
                {
                    logger.LogInformation("Seeding Permissions...");
                    var permissions = new List<Permission>
                    {
                        // User Management
                        new Permission { PermissionId = 1, PermissionName = "user.view-any", Description = "View all users", Group = "User", CreatedAt = DateTime.UtcNow },
                        new Permission { PermissionId = 2, PermissionName = "user.view", Description = "View user detail", Group = "User", CreatedAt = DateTime.UtcNow },
                        new Permission { PermissionId = 3, PermissionName = "user.create", Description = "Create new user", Group = "User", CreatedAt = DateTime.UtcNow },
                        new Permission { PermissionId = 4, PermissionName = "user.update", Description = "Update user", Group = "User", CreatedAt = DateTime.UtcNow },
                        new Permission { PermissionId = 5, PermissionName = "user.delete", Description = "Delete user", Group = "User", CreatedAt = DateTime.UtcNow },
                        
                        // Role Management
                        new Permission { PermissionId = 6, PermissionName = "role.view-any", Description = "View all roles", Group = "Role", CreatedAt = DateTime.UtcNow },
                        new Permission { PermissionId = 7, PermissionName = "role.view", Description = "View role detail", Group = "Role", CreatedAt = DateTime.UtcNow },
                        new Permission { PermissionId = 8, PermissionName = "role.create", Description = "Create new role", Group = "Role", CreatedAt = DateTime.UtcNow },
                        new Permission { PermissionId = 9, PermissionName = "role.update", Description = "Update role", Group = "Role", CreatedAt = DateTime.UtcNow },
                        new Permission { PermissionId = 10, PermissionName = "role.delete", Description = "Delete role", Group = "Role", CreatedAt = DateTime.UtcNow },
                        
                        // Permission Management
                        new Permission { PermissionId = 11, PermissionName = "permission.view", Description = "View permissions", Group = "Permission", CreatedAt = DateTime.UtcNow },
                        new Permission { PermissionId = 12, PermissionName = "permission.edit", Description = "Create, update, and delete permissions", Group = "Permission", CreatedAt = DateTime.UtcNow },
                        new Permission { PermissionId = 23, PermissionName = "permission.create", Description = "Create new permissions", Group = "Permission", CreatedAt = DateTime.UtcNow },

                        // Call Record Management
                        new Permission { PermissionId = 13, PermissionName = "callrecord.import", Description = "Import call records from CSV", Group = "CallRecord", CreatedAt = DateTime.UtcNow },
                        new Permission { PermissionId = 14, PermissionName = "callrecord.view-any", Description = "View all call records", Group = "CallRecord", CreatedAt = DateTime.UtcNow },
                        new Permission { PermissionId = 15, PermissionName = "callrecord.view", Description = "View call record details and summaries", Group = "CallRecord", CreatedAt = DateTime.UtcNow },
                        new Permission { PermissionId = 16, PermissionName = "callrecord.export-excel", Description = "Export call records to Excel", Group = "CallRecord", CreatedAt = DateTime.UtcNow },
                        new Permission { PermissionId = 17, PermissionName = "callrecord.export-csv", Description = "Export call records to CSV", Group = "CallRecord", CreatedAt = DateTime.UtcNow },
                        new Permission { PermissionId = 18, PermissionName = "callrecord.delete", Description = "Delete call records by date", Group = "CallRecord", CreatedAt = DateTime.UtcNow },
                        
                        // System Management
                        new Permission { PermissionId = 19, PermissionName = "system.settings.view", Description = "View system settings", Group = "System", CreatedAt = DateTime.UtcNow },
                        new Permission { PermissionId = 20, PermissionName = "system.settings.edit", Description = "Edit system settings", Group = "System", CreatedAt = DateTime.UtcNow },
                        new Permission { PermissionId = 21, PermissionName = "system.audit.view", Description = "View audit logs", Group = "System", CreatedAt = DateTime.UtcNow },
                        new Permission { PermissionId = 22, PermissionName = "delete.all-data", Description = "Reset all data (DANGER)", Group = "System", CreatedAt = DateTime.UtcNow }
                    };
                    await context.Permissions.AddRangeAsync(permissions);
                    await context.SaveChangesAsync();
                    logger.LogInformation("✅ Permissions seeded successfully");
                }

                // 3. Seed RolePermissions
                if (!await context.RolePermissions.AnyAsync())
                {
                    logger.LogInformation("Seeding Role Permissions...");
                    var rolePermissions = new List<RolePermission>();

                    // Super Admin gets ALL permissions (1-22)
                    for (int i = 1; i <= 22; i++)
                    {
                        rolePermissions.Add(new RolePermission
                        {
                            RoleId = 1,
                            PermissionId = i,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    // Admin gets limited permissions
                    var adminPermissions = new[] { 1, 2, 3, 4, 6, 7, 11, 13, 14, 15, 16, 17, 18, 19 };
                    foreach (var permId in adminPermissions)
                    {
                        rolePermissions.Add(new RolePermission
                        {
                            RoleId = 2,
                            PermissionId = permId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    // User gets basic permissions
                    var userPermissions = new[] { 2, 14, 15 };
                    foreach (var permId in userPermissions)
                    {
                        rolePermissions.Add(new RolePermission
                        {
                            RoleId = 3,
                            PermissionId = permId,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    await context.RolePermissions.AddRangeAsync(rolePermissions);
                    await context.SaveChangesAsync();
                    logger.LogInformation("✅ Role Permissions seeded successfully");
                }

                // 4. Seed Super Admin User
                if (!await context.Users.AnyAsync())
                {
                    logger.LogInformation("Seeding Super Admin user...");
                    var superAdmin = new User
                    {
                        Username = "superadmin",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password!"),
                        FullName = "System Administrator",
                        Email = "jupriekapratama@gmail.com",
                        IsActive = true,
                        RoleId = 1,
                        CreatedAt = DateTime.UtcNow
                    };
                    await context.Users.AddAsync(superAdmin);
                    await context.SaveChangesAsync();
                    logger.LogInformation("✅ Super Admin user seeded successfully");
                    logger.LogWarning("⚠️ Default credentials - Username: admin, Password: Admin123!");
                    logger.LogWarning("⚠️ PLEASE CHANGE PASSWORD AFTER FIRST LOGIN!");
                }

                logger.LogInformation("🎉 Database seeding completed successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Error seeding database");
                throw;
            }
        }
    }
}