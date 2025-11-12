using Microsoft.EntityFrameworkCore;
using Pm.Data;
using Pm.DTOs;
using Pm.Models;

namespace Pm.Services
{

    public class RolePermissionService : IRolePermissionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<RolePermissionService> _logger;

        public RolePermissionService(AppDbContext context, ILogger<RolePermissionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<RolePermissionDto>> GetAllRolePermissionsAsync()
        {
            var rolePermissions = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .OrderBy(rp => rp.RoleId)
                .ThenBy(rp => rp.Permission.Group)
                .ThenBy(rp => rp.Permission.PermissionName)
                .Select(rp => new RolePermissionDto
                {
                    RolePermissionId = rp.RolePermissionId,
                    RoleId = rp.RoleId,
                    RoleName = rp.Role.RoleName,
                    PermissionId = rp.PermissionId,
                    PermissionName = rp.Permission.PermissionName,
                    PermissionGroup = rp.Permission.Group,
                    CreatedAt = rp.CreatedAt
                })
                .ToListAsync();

            return rolePermissions;
        }

        public async Task<RolePermissionDetailDto?> GetPermissionsByRoleAsync(int roleId)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.RoleId == roleId);

            if (role == null)
            {
                return null;
            }

            // Get all available permissions
            var allPermissions = await _context.Permissions
                .OrderBy(p => p.Group)
                .ThenBy(p => p.PermissionName)
                .ToListAsync();

            // Get assigned permission IDs for this role
            var assignedPermissionIds = role.RolePermissions
                .Select(rp => rp.PermissionId)
                .ToHashSet();

            return new RolePermissionDetailDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                RoleDescription = role.Description,
                AssignedPermissions = role.RolePermissions
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
                    .ToList(),
                AvailablePermissions = allPermissions
                    .Where(p => !assignedPermissionIds.Contains(p.PermissionId))
                    .Select(p => new PermissionDto
                    {
                        PermissionId = p.PermissionId,
                        PermissionName = p.PermissionName,
                        Description = p.Description,
                        Group = p.Group,
                        CreatedAt = p.CreatedAt
                    })
                    .ToList()
            };
        }

        public async Task<List<RoleDto>?> GetRolesByPermissionAsync(int permissionId)
        {
            var permission = await _context.Permissions
                .Include(p => p.RolePermissions)
                    .ThenInclude(rp => rp.Role)
                        .ThenInclude(r => r.Users)
                .FirstOrDefaultAsync(p => p.PermissionId == permissionId);

            if (permission == null)
            {
                return null;
            }

            var roles = permission.RolePermissions
                .Select(rp => new RoleDto
                {
                    RoleId = rp.Role.RoleId,
                    RoleName = rp.Role.RoleName,
                    Description = rp.Role.Description,
                    IsActive = rp.Role.IsActive,
                    UserCount = rp.Role.Users.Count,
                    CreatedAt = rp.Role.CreatedAt
                })
                .OrderBy(r => r.RoleId)
                .ToList();

            return roles;
        }

        public async Task<RolePermissionDto?> AddPermissionToRoleAsync(CreateRolePermissionDto dto)
        {
            // Prevent modifying Super Admin permissions
            if (dto.RoleId == 1)
            {
                throw new Exception("Tidak dapat mengubah permissions untuk Super Admin");
            }

            // Check if role exists
            var roleExists = await _context.Roles.AnyAsync(r => r.RoleId == dto.RoleId);
            if (!roleExists)
            {
                throw new Exception("Role tidak ditemukan");
            }

            // Check if permission exists
            var permission = await _context.Permissions.FindAsync(dto.PermissionId);
            if (permission == null)
            {
                throw new Exception("Permission tidak ditemukan");
            }

            // Check if already exists
            var existing = await _context.RolePermissions
                .AnyAsync(rp => rp.RoleId == dto.RoleId && rp.PermissionId == dto.PermissionId);

            if (existing)
            {
                return null; // Already exists
            }

            var rolePermission = new RolePermission
            {
                RoleId = dto.RoleId,
                PermissionId = dto.PermissionId,
                CreatedAt = DateTime.UtcNow
            };

            _context.RolePermissions.Add(rolePermission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Permission {PermissionId} added to Role {RoleId}", dto.PermissionId, dto.RoleId);

            // Load related data for response
            await _context.Entry(rolePermission)
                .Reference(rp => rp.Role)
                .LoadAsync();
            await _context.Entry(rolePermission)
                .Reference(rp => rp.Permission)
                .LoadAsync();

            return new RolePermissionDto
            {
                RolePermissionId = rolePermission.RolePermissionId,
                RoleId = rolePermission.RoleId,
                RoleName = rolePermission.Role.RoleName,
                PermissionId = rolePermission.PermissionId,
                PermissionName = rolePermission.Permission.PermissionName,
                PermissionGroup = rolePermission.Permission.Group,
                CreatedAt = rolePermission.CreatedAt
            };
        }

        public async Task<RolePermissionDetailDto?> UpdateRolePermissionsAsync(int roleId, List<int> permissionIds)
        {
            // Prevent modifying Super Admin permissions
            if (roleId == 1)
            {
                throw new Exception("Tidak dapat mengubah permissions untuk Super Admin");
            }

            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.RoleId == roleId);

            if (role == null)
            {
                return null;
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

                // Return updated permissions
                return await GetPermissionsByRoleAsync(roleId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating permissions for Role: {RoleId}", roleId);
                throw;
            }
        }

        public async Task<bool> RemovePermissionFromRoleAsync(int roleId, int permissionId)
        {
            // Prevent modifying Super Admin permissions
            if (roleId == 1)
            {
                throw new Exception("Tidak dapat mengubah permissions untuk Super Admin");
            }

            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (rolePermission == null)
            {
                return false;
            }

            _context.RolePermissions.Remove(rolePermission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Permission {PermissionId} removed from Role {RoleId}", permissionId, roleId);
            return true;
        }

        public async Task<List<RolePermissionMatrixDto>> GetPermissionMatrixAsync()
        {
            var roles = await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .OrderBy(r => r.RoleId)
                .ToListAsync();

            var allPermissions = await _context.Permissions
                .OrderBy(p => p.Group)
                .ThenBy(p => p.PermissionName)
                .ToListAsync();

            var matrix = roles.Select(role => new RolePermissionMatrixDto
            {
                RoleId = role.RoleId,
                RoleName = role.RoleName,
                RoleDescription = role.Description,
                IsActive = role.IsActive,
                Permissions = allPermissions.Select(permission => new PermissionStatusDto
                {
                    PermissionId = permission.PermissionId,
                    PermissionName = permission.PermissionName,
                    PermissionGroup = permission.Group,
                    IsAssigned = role.RolePermissions.Any(rp => rp.PermissionId == permission.PermissionId)
                }).ToList()
            }).ToList();

            return matrix;
        }
    }
}