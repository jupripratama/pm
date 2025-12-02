using System.ComponentModel.DataAnnotations;

namespace Pm.DTOs
{
    public class RolePermissionDto
    {
        public int RolePermissionId { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public int PermissionId { get; set; }
        public string PermissionName { get; set; } = string.Empty;
        public string? PermissionGroup { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateRolePermissionDto
    {
        [Required(ErrorMessage = "Role ID wajib diisi")]
        public int RoleId { get; set; }

        [Required(ErrorMessage = "Permission ID wajib diisi")]
        public int PermissionId { get; set; }
    }

    public class RolePermissionDetailDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? RoleDescription { get; set; }
        public List<PermissionDto> AssignedPermissions { get; set; } = new();
        public List<PermissionDto> AvailablePermissions { get; set; } = new();
    }

    public class RolePermissionMatrixDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? RoleDescription { get; set; }
        public bool IsActive { get; set; }
        public List<PermissionStatusDto> Permissions { get; set; } = new();
    }

    public class PermissionStatusDto
    {
        public int PermissionId { get; set; }
        public string PermissionName { get; set; } = string.Empty;
        public string? PermissionGroup { get; set; }
        public bool IsAssigned { get; set; }
    }


}