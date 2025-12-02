using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.Models;

namespace Pm.Services
{


    public class RoleService : IRoleService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RoleService> _logger;

        public RoleService(AppDbContext context, ILogger<RoleService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<RoleDto>> GetAllRolesAsync()
        {
            var roles = await _context.Roles
                .Include(r => r.Users)
                .OrderBy(r => r.RoleId)
                .Select(r => new RoleDto
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName,
                    Description = r.Description,
                    IsActive = r.IsActive,
                    UserCount = r.Users.Count,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return roles;
        }

        public async Task<RoleDto?> GetRoleByIdAsync(int roleId)
        {
            var role = await _context.Roles
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.RoleId == roleId);

            if (role == null)
            {
                return null;
            }

            return new RoleDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Description = role.Description,
                IsActive = role.IsActive,
                UserCount = role.Users.Count,
                CreatedAt = role.CreatedAt
            };
        }

        public async Task<RoleDto> CreateRoleAsync(CreateRoleDto dto)
        {
            // Check if role name exists
            if (await IsRoleNameExistsAsync(dto.RoleName))
            {
                throw new Exception("Role name sudah digunakan");
            }

            var role = new Role
            {
                RoleName = dto.RoleName,
                Description = dto.Description,
                IsActive = dto.IsActive ?? true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            // Add default permissions if provided
            if (dto.PermissionIds != null && dto.PermissionIds.Any())
            {
                var validPermissionIds = await _context.Permissions
                    .Where(p => dto.PermissionIds.Contains(p.PermissionId))
                    .Select(p => p.PermissionId)
                    .ToListAsync();

                if (validPermissionIds.Count != dto.PermissionIds.Count)
                {
                    _logger.LogWarning("Some permission IDs were invalid and skipped");
                }

                var rolePermissions = validPermissionIds.Select(permissionId => new RolePermission
                {
                    RoleId = role.RoleId,
                    PermissionId = permissionId,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.RolePermissions.AddRangeAsync(rolePermissions);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Role created successfully: {RoleName}", role.RoleName);

            return new RoleDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Description = role.Description,
                IsActive = role.IsActive,
                UserCount = 0,
                CreatedAt = role.CreatedAt
            };
        }

        public async Task<RoleDto?> UpdateRoleAsync(int roleId, UpdateRoleDto dto)
        {
            var role = await _context.Roles
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.RoleId == roleId);

            if (role == null)
            {
                _logger.LogWarning("Role not found for update: {RoleId}", roleId);
                return null;
            }

            // Prevent modifying Super Admin role
            if (roleId == 1)
            {
                throw new Exception("Tidak dapat mengubah Super Admin role");
            }

            // Track changes untuk update
            _context.Entry(role).State = EntityState.Modified;

            // Check role name uniqueness if changed
            if (!string.IsNullOrEmpty(dto.RoleName) && dto.RoleName != role.RoleName)
            {
                if (await IsRoleNameExistsAsync(dto.RoleName, roleId))
                {
                    throw new Exception("Role name sudah digunakan");
                }
                role.RoleName = dto.RoleName;
            }

            // Update other fields if provided
            if (dto.Description != null)
            {
                role.Description = dto.Description;
            }

            if (dto.IsActive.HasValue)
            {
                role.IsActive = dto.IsActive.Value;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Role updated successfully: {RoleId}", roleId);

            return new RoleDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Description = role.Description,
                IsActive = role.IsActive,
                UserCount = role.Users.Count,
                CreatedAt = role.CreatedAt
            };
        }

        public async Task<bool> DeleteRoleAsync(int roleId)
        {
            var role = await _context.Roles
                .Include(r => r.Users)
                .FirstOrDefaultAsync(r => r.RoleId == roleId);

            if (role == null)
            {
                _logger.LogWarning("Role not found for deletion: {RoleId}", roleId);
                return false;
            }

            // Prevent deleting Super Admin role
            if (roleId == 1)
            {
                throw new Exception("Tidak dapat menghapus Super Admin role");
            }

            // Check if role has users
            if (role.Users.Any())
            {
                throw new Exception($"Tidak dapat menghapus role yang masih memiliki {role.Users.Count} user");
            }

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Role deleted successfully: {RoleId}", roleId);
            return true;
        }

        public async Task<RoleWithPermissionsDto?> GetRoleWithPermissionsAsync(int roleId)
        {
            var role = await _context.Roles
                .Include(r => r.Users)
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.RoleId == roleId);

            if (role == null)
            {
                return null;
            }

            return new RoleWithPermissionsDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                Description = role.Description,
                IsActive = role.IsActive,
                UserCount = role.Users.Count,
                CreatedAt = role.CreatedAt,
                Permissions = role.RolePermissions
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
                    .ToList()
            };
        }

        public async Task<bool> IsRoleNameExistsAsync(string roleName, int? excludeRoleId = null)
        {
            var query = _context.Roles.Where(r => r.RoleName == roleName);
            if (excludeRoleId.HasValue)
            {
                query = query.Where(r => r.RoleId != excludeRoleId.Value);
            }
            return await query.AnyAsync();
        }
    }
}