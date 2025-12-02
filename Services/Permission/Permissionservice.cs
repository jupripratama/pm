using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.Models;

namespace Pm.Services
{


    public class PermissionService : IPermissionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(AppDbContext context, ILogger<PermissionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<PermissionDto>> GetAllPermissionsAsync()
        {
            var permissions = await _context.Permissions
                .OrderBy(p => p.Group)
                .ThenBy(p => p.PermissionName)
                .Select(p => new PermissionDto
                {
                    PermissionId = p.PermissionId,
                    PermissionName = p.PermissionName,
                    Description = p.Description,
                    Group = p.Group,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return permissions;
        }

        public async Task<List<PermissionDto>> GetPermissionsByGroupAsync(string group)
        {
            var permissions = await _context.Permissions
                .Where(p => p.Group == group)
                .OrderBy(p => p.PermissionName)
                .Select(p => new PermissionDto
                {
                    PermissionId = p.PermissionId,
                    PermissionName = p.PermissionName,
                    Description = p.Description,
                    Group = p.Group,
                    CreatedAt = p.CreatedAt
                })
                .ToListAsync();

            return permissions;
        }

        public async Task<List<string>> GetPermissionGroupsAsync()
        {
            var groups = await _context.Permissions
                .Where(p => p.Group != null)
                .Select(p => p.Group!)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            return groups;
        }

        public async Task<List<PermissionDto>?> GetPermissionsByRoleAsync(int roleId)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.RoleId == roleId);

            if (role == null)
            {
                return null;
            }

            var permissions = role.RolePermissions
                .Select(rp => new PermissionDto
                {
                    PermissionId = rp.Permission.PermissionId,
                    PermissionName = rp.Permission.PermissionName,
                    Description = rp.Permission.Description,
                    Group = rp.Permission.Group,
                    CreatedAt = rp.Permission.CreatedAt
                })
                .OrderBy(p => p.Group)
                .ThenBy(p => p.PermissionName)
                .ToList();

            return permissions;
        }

        public async Task<List<PermissionDto>?> GetPermissionsByUserAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                    .ThenInclude(r => r!.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null || user.Role == null)
            {
                return null;
            }

            var permissions = user.Role.RolePermissions
                .Select(rp => new PermissionDto
                {
                    PermissionId = rp.Permission.PermissionId,
                    PermissionName = rp.Permission.PermissionName,
                    Description = rp.Permission.Description,
                    Group = rp.Permission.Group,
                    CreatedAt = rp.Permission.CreatedAt
                })
                .OrderBy(p => p.Group)
                .ThenBy(p => p.PermissionName)
                .ToList();

            return permissions;
        }

        public async Task<bool> UpdateRolePermissionsAsync(int roleId, List<int> permissionIds)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.RoleId == roleId);

            if (role == null)
            {
                _logger.LogWarning("Role not found for permission update: {RoleId}", roleId);
                return false;
            }

            // Prevent modifying Super Admin permissions
            if (roleId == 1)
            {
                throw new Exception("Tidak dapat mengubah permissions untuk Super Admin");
            }

            // Validate all permission IDs exist
            var existingPermissionIds = await _context.Permissions
                .Where(p => permissionIds.Contains(p.PermissionId))
                .Select(p => p.PermissionId)
                .ToListAsync();

            if (existingPermissionIds.Count != permissionIds.Count)
            {
                throw new Exception("Satu atau lebih Permission ID tidak valid");
            }

            // Begin transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Remove existing role permissions
                _context.RolePermissions.RemoveRange(role.RolePermissions);
                await _context.SaveChangesAsync();

                // Add new role permissions
                var newRolePermissions = permissionIds.Select(permissionId => new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.RolePermissions.AddRangeAsync(newRolePermissions);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                _logger.LogInformation("Permissions updated successfully for Role: {RoleId}", roleId);
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating permissions for Role: {RoleId}", roleId);
                throw;
            }
        }

        public async Task<PermissionDto> CreatePermissionAsync(CreatePermissionDto dto)
        {
            // Check if permission name exists
            if (await IsPermissionNameExistsAsync(dto.PermissionName))
            {
                throw new Exception("Permission name sudah digunakan");
            }

            var permission = new Permission
            {
                PermissionName = dto.PermissionName,
                Description = dto.Description,
                Group = dto.Group,
                CreatedAt = DateTime.UtcNow
            };

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Permission created successfully: {PermissionName}", permission.PermissionName);

            return new PermissionDto
            {
                PermissionId = permission.PermissionId,
                PermissionName = permission.PermissionName,
                Description = permission.Description,
                Group = permission.Group,
                CreatedAt = permission.CreatedAt
            };
        }

        public async Task<PermissionDto?> UpdatePermissionAsync(int permissionId, UpdatePermissionDto dto)
        {
            var permission = await _context.Permissions.FindAsync(permissionId);
            if (permission == null)
            {
                _logger.LogWarning("Permission not found for update: {PermissionId}", permissionId);
                return null;
            }

            // Track changes untuk update
            _context.Entry(permission).State = EntityState.Modified;

            // Check permission name uniqueness if changed
            if (!string.IsNullOrEmpty(dto.PermissionName) && dto.PermissionName != permission.PermissionName)
            {
                if (await IsPermissionNameExistsAsync(dto.PermissionName, permissionId))
                {
                    throw new Exception("Permission name sudah digunakan");
                }
                permission.PermissionName = dto.PermissionName;
            }

            // Update other fields if provided
            if (dto.Description != null)
            {
                permission.Description = dto.Description;
            }

            if (!string.IsNullOrEmpty(dto.Group))
            {
                permission.Group = dto.Group;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Permission updated successfully: {PermissionId}", permissionId);

            return new PermissionDto
            {
                PermissionId = permission.PermissionId,
                PermissionName = permission.PermissionName,
                Description = permission.Description,
                Group = permission.Group,
                CreatedAt = permission.CreatedAt
            };
        }

        public async Task<bool> DeletePermissionAsync(int permissionId)
        {
            var permission = await _context.Permissions
                .Include(p => p.RolePermissions)
                .FirstOrDefaultAsync(p => p.PermissionId == permissionId);

            if (permission == null)
            {
                _logger.LogWarning("Permission not found for deletion: {PermissionId}", permissionId);
                return false;
            }

            // Check if permission is in use
            if (permission.RolePermissions.Any())
            {
                throw new Exception("Tidak dapat menghapus permission yang sedang digunakan oleh role");
            }

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Permission deleted successfully: {PermissionId}", permissionId);
            return true;
        }

        public async Task<bool> IsPermissionNameExistsAsync(string permissionName, int? excludePermissionId = null)
        {
            var query = _context.Permissions.Where(p => p.PermissionName == permissionName);
            if (excludePermissionId.HasValue)
            {
                query = query.Where(p => p.PermissionId != excludePermissionId.Value);
            }
            return await query.AnyAsync();
        }
    }
}