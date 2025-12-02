using Pm.DTOs;
namespace Pm.Services
{
    public interface IRolePermissionService
    {
        Task<List<RolePermissionDto>> GetAllRolePermissionsAsync();
        Task<RolePermissionDetailDto?> GetPermissionsByRoleAsync(int roleId);
        Task<List<RoleDto>?> GetRolesByPermissionAsync(int permissionId);
        Task<RolePermissionDto?> AddPermissionToRoleAsync(CreateRolePermissionDto dto);
        Task<RolePermissionDetailDto?> UpdateRolePermissionsAsync(int roleId, List<int> permissionIds);
        Task<bool> RemovePermissionFromRoleAsync(int roleId, int permissionId);
        Task<List<RolePermissionMatrixDto>> GetPermissionMatrixAsync();
    }

}