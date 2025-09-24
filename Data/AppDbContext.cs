using Microsoft.EntityFrameworkCore;
using Pm.Models;

namespace Pm.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User Configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                
                entity.HasOne(u => u.Role)
                      .WithMany(r => r.Users)
                      .HasForeignKey(u => u.RoleId)
                      .OnDelete(DeleteBehavior.Restrict);

            });

            // Role Configuration
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(e => e.RoleId);
                entity.HasIndex(e => e.RoleName).IsUnique();
            });

            // Permission Configuration
            modelBuilder.Entity<Permission>(entity =>
            {
                entity.HasKey(e => e.PermissionId);
                entity.HasIndex(e => e.PermissionName).IsUnique();
            });

            // RolePermission Configuration
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(e => e.RolePermissionId);
                
                entity.HasOne(rp => rp.Role)
                      .WithMany(r => r.RolePermissions)
                      .HasForeignKey(rp => rp.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rp => rp.Permission)
                      .WithMany(p => p.RolePermissions)
                      .HasForeignKey(rp => rp.PermissionId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Unique constraint untuk kombinasi RoleId dan PermissionId
                entity.HasIndex(e => new { e.RoleId, e.PermissionId }).IsUnique();
            });

            // Seed Data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Roles
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "Super Admin", Description = "Full system access", CreatedAt = DateTime.UtcNow },
                new Role { RoleId = 2, RoleName = "Admin", Description = "Administrative access", CreatedAt = DateTime.UtcNow },
                new Role { RoleId = 3, RoleName = "User", Description = "Standard user access", CreatedAt = DateTime.UtcNow }
            );

            // Seed Permissions
            var permissions = new[]
            {
                // User permissions
                new Permission { PermissionId = 1, PermissionName = "user.view-any", Description = "View all users", Group = "User", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 2, PermissionName = "user.view", Description = "View user detail", Group = "User", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 3, PermissionName = "user.create", Description = "Create new user", Group = "User", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 4, PermissionName = "user.update", Description = "Update user", Group = "User", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 5, PermissionName = "user.delete", Description = "Delete user", Group = "User", CreatedAt = DateTime.UtcNow },
                
                // Role permissions
                new Permission { PermissionId = 6, PermissionName = "role.view-any", Description = "View all roles", Group = "Role", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 7, PermissionName = "role.view", Description = "View role detail", Group = "Role", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 8, PermissionName = "role.create", Description = "Create new role", Group = "Role", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 9, PermissionName = "role.update", Description = "Update role", Group = "Role", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 10, PermissionName = "role.delete", Description = "Delete role", Group = "Role", CreatedAt = DateTime.UtcNow },
                
                // Permission permissions
                new Permission { PermissionId = 11, PermissionName = "permission.view", Description = "View permissions", Group = "Permission", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 12, PermissionName = "permission.edit", Description = "Edit permissions", Group = "Permission", CreatedAt = DateTime.UtcNow },
            };

            modelBuilder.Entity<Permission>().HasData(permissions);

            // Seed RolePermissions (Super Admin gets all permissions)
            var rolePermissions = new List<RolePermission>();
            for (int i = 1; i <= permissions.Length; i++)
            {
                rolePermissions.Add(new RolePermission 
                { 
                    RolePermissionId = i, 
                    RoleId = 1, 
                    PermissionId = i, 
                    CreatedAt = DateTime.UtcNow 
                });
            }

            // Admin gets limited permissions
            rolePermissions.AddRange(new[]
            {
                new RolePermission { RolePermissionId = 13, RoleId = 2, PermissionId = 1, CreatedAt = DateTime.UtcNow }, // user.view-any
                new RolePermission { RolePermissionId = 14, RoleId = 2, PermissionId = 2, CreatedAt = DateTime.UtcNow }, // user.view
                new RolePermission { RolePermissionId = 15, RoleId = 2, PermissionId = 3, CreatedAt = DateTime.UtcNow }, // user.create
                new RolePermission { RolePermissionId = 16, RoleId = 2, PermissionId = 4, CreatedAt = DateTime.UtcNow }, // user.update
            });

            // User gets basic permissions
            rolePermissions.AddRange(new[]
            {
                new RolePermission { RolePermissionId = 17, RoleId = 3, PermissionId = 2, CreatedAt = DateTime.UtcNow }, // user.view
            });

            modelBuilder.Entity<RolePermission>().HasData(rolePermissions);

            // Seed Default Super Admin User
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    Username = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"), // Default password
                    FullName = "System Administrator",
                    Email = "admin@yourdomain.com",
                    IsActive = true,
                    RoleId = 1,
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
    }
}