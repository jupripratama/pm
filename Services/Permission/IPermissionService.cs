using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Models;

namespace Pm.Services
{
    public interface IPermissionService
    {
        Task<List<PermissionDto>> GetAllPermissionsAsync();
        Task<List<PermissionDto>> GetPermissionsByGroupAsync(string group);
        Task<List<string>> GetPermissionGroupsAsync();
        Task<List<PermissionDto>?> GetPermissionsByRoleAsync(int roleId);
        Task<List<PermissionDto>?> GetPermissionsByUserAsync(int userId);
        Task<bool> UpdateRolePermissionsAsync(int roleId, List<int> permissionIds);
        Task<PermissionDto> CreatePermissionAsync(CreatePermissionDto dto);
        Task<PermissionDto?> UpdatePermissionAsync(int permissionId, UpdatePermissionDto dto);
        Task<bool> DeletePermissionAsync(int permissionId);
        Task<bool> IsPermissionNameExistsAsync(string permissionName, int? excludePermissionId = null);
    }
}