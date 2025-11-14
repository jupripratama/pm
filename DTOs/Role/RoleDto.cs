using System.ComponentModel.DataAnnotations;

namespace Pm.DTOs
{
    public class RoleDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int UserCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateRoleDto
    {
        [Required(ErrorMessage = "Role name wajib diisi")]
        [StringLength(50, ErrorMessage = "Role name maksimal 50 karakter")]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Description maksimal 255 karakter")]
        public string? Description { get; set; }

        public bool? IsActive { get; set; } = true;

        public List<int>? PermissionIds { get; set; }
    }

    public class UpdateRoleDto
    {
        [StringLength(50, ErrorMessage = "Role name maksimal 50 karakter")]
        public string? RoleName { get; set; }

        [StringLength(255, ErrorMessage = "Description maksimal 255 karakter")]
        public string? Description { get; set; }

        public bool? IsActive { get; set; }

        public int RoleId { get; set; }
    }

    public class RoleWithPermissionsDto : RoleDto
    {
        public List<PermissionDto> Permissions { get; set; } = new();
    }
}