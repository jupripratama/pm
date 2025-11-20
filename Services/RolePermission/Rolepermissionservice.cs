using System.Security.Claims;
using Microsoft.AspNetCore.Http;
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
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RolePermissionService(
            AppDbContext context,
            ILogger<RolePermissionService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        private bool IsCurrentUserSuperAdmin()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity?.IsAuthenticated == true)
            {
                _logger.LogWarning("User tidak terautentikasi");
                return false;
            }

            // Log semua claims untuk debug
            _logger.LogInformation("=== Debug Claims ===");
            foreach (var claim in user.Claims)
            {
                _logger.LogInformation("Claim: {Type} = {Value}", claim.Type, claim.Value);
            }

            var roleId = user.FindFirst("RoleId")?.Value;
            var roleName = user.FindFirst("RoleName")?.Value;

            _logger.LogInformation("RoleId: {RoleId}, RoleName: {RoleName}", roleId, roleName);

            return roleId == "1" || roleName == "Super Admin";
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
            if (dto.RoleId == 1)
            {
                if (!IsCurrentUserSuperAdmin())
                    throw new UnauthorizedAccessException("Hanya Super Admin yang dapat mengubah permissions role Super Admin");
            }

            var roleExists = await _context.Roles.AnyAsync(r => r.RoleId == dto.RoleId);
            if (!roleExists) throw new Exception("Role tidak ditemukan");

            var permission = await _context.Permissions.FindAsync(dto.PermissionId);
            if (permission == null) throw new Exception("Permission tidak ditemukan");

            var existing = await _context.RolePermissions
                .AnyAsync(rp => rp.RoleId == dto.RoleId && rp.PermissionId == dto.PermissionId);
            if (existing) return null;

            var rolePermission = new RolePermission
            {
                RoleId = dto.RoleId,
                PermissionId = dto.PermissionId,
                CreatedAt = DateTime.UtcNow
            };

            _context.RolePermissions.Add(rolePermission);
            await _context.SaveChangesAsync();

            await _context.Entry(rolePermission).Reference(rp => rp.Role).LoadAsync();
            await _context.Entry(rolePermission).Reference(rp => rp.Permission).LoadAsync();

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
            // INI YANG BENAR — HANYA CEK KALAU roleId == 1
            if (roleId == 1)
            {
                if (!IsCurrentUserSuperAdmin())
                    throw new UnauthorizedAccessException("Hanya Super Admin yang dapat mengubah permissions role Super Admin");
            }

            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.RoleId == roleId);

            if (role == null) return null;

            // Validasi semua permission ID valid
            var existingIds = await _context.Permissions
                .Where(p => permissionIds.Contains(p.PermissionId))
                .Select(p => p.PermissionId)
                .ToListAsync();

            if (existingIds.Count != permissionIds.Count)
            {
                var invalid = permissionIds.Except(existingIds).ToList();
                throw new Exception($"Permission tidak valid: {string.Join(", ", invalid)}");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.RolePermissions.RemoveRange(role.RolePermissions);
                await _context.SaveChangesAsync();

                var newPermissions = permissionIds.Select(pid => new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = pid,
                    CreatedAt = DateTime.UtcNow
                }).ToList();

                await _context.RolePermissions.AddRangeAsync(newPermissions);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetPermissionsByRoleAsync(roleId);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<bool> RemovePermissionFromRoleAsync(int roleId, int permissionId)
        {
            if (roleId == 1)
            {
                if (!IsCurrentUserSuperAdmin())
                    throw new UnauthorizedAccessException("Hanya Super Admin yang dapat mengubah permissions role Super Admin");
            }

            var rp = await _context.RolePermissions
                .FirstOrDefaultAsync(x => x.RoleId == roleId && x.PermissionId == permissionId);

            if (rp == null) return false;

            _context.RolePermissions.Remove(rp);
            await _context.SaveChangesAsync();
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