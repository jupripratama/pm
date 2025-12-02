using Pm.DTOs;
using Pm.DTOs.Common;
using Pm.Models;

namespace Pm.Services
{
    public interface IRoleService
    {
        Task<List<RoleDto>> GetAllRolesAsync();
        Task<RoleDto?> GetRoleByIdAsync(int roleId);
        Task<RoleDto> CreateRoleAsync(CreateRoleDto dto);
        Task<RoleDto?> UpdateRoleAsync(int roleId, UpdateRoleDto dto);
        Task<bool> DeleteRoleAsync(int roleId);
        Task<RoleWithPermissionsDto?> GetRoleWithPermissionsAsync(int roleId);
        Task<bool> IsRoleNameExistsAsync(string roleName, int? excludeRoleId = null);
    }
}