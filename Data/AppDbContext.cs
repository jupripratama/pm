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
        public DbSet<CallRecord> CallRecords { get; set; }
        public DbSet<CallSummary> CallSummaries { get; set; }
        public DbSet<FleetStatistic> FleetStatistics { get; set; }
        public DbSet<FileImportHistory> FileImportHistories { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // if (!optionsBuilder.IsConfigured)
            // {
            //     // Add performance optimizations for large data
            //     optionsBuilder.EnableSensitiveDataLogging(false)
            //                 .EnableDetailedErrors(false);
            // }

            base.OnConfiguring(optionsBuilder);

            // Optimizations untuk bulk operations
            optionsBuilder
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking) // Default no tracking
                .EnableSensitiveDataLogging(false) // Disable untuk performa
                .EnableDetailedErrors(false); // Disable untuk performa
        }
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

            // CallRecord Configuration dengan optimasi
            modelBuilder.Entity<CallRecord>(entity =>
            {
                entity.HasKey(e => e.CallRecordId);

                // Composite index untuk query yang sering dipakai
                entity.HasIndex(e => new { e.CallDate, e.CallTime })
                    .HasDatabaseName("IX_CallRecord_DateTime");

                entity.HasIndex(e => e.CallCloseReason)
                    .HasDatabaseName("IX_CallRecord_CloseReason");

                entity.HasIndex(e => e.CallDate)
                    .HasDatabaseName("IX_CallRecord_Date");

                // Index untuk hour-based queries
                entity.HasIndex("CallDate", "CallTime")
                    .HasDatabaseName("IX_CallRecord_HourQuery");
            });

            // CallSummary dengan partitioning hint
            modelBuilder.Entity<CallSummary>(entity =>
            {
                entity.HasKey(e => e.CallSummaryId);
                entity.HasIndex(e => new { e.SummaryDate, e.HourGroup })
                    .IsUnique()
                    .HasDatabaseName("IX_CallSummary_DateHour");

                entity.Property(e => e.TEBusyPercent).HasColumnType("decimal(5,2)");
                entity.Property(e => e.SysBusyPercent).HasColumnType("decimal(5,2)");
                entity.Property(e => e.OthersPercent).HasColumnType("decimal(5,2)");
            });



            // Seed Data
            // SeedData(modelBuilder);
        }


    }
}