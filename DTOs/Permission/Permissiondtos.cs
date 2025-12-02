using System.ComponentModel.DataAnnotations;

namespace Pm.DTOs
{
    public class PermissionDto
    {
        public int PermissionId { get; set; }
        public string PermissionName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Group { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePermissionDto
    {
        [Required(ErrorMessage = "Permission name wajib diisi")]
        [StringLength(100, ErrorMessage = "Permission name maksimal 100 karakter")]
        public string PermissionName { get; set; } = string.Empty;

        [StringLength(255, ErrorMessage = "Description maksimal 255 karakter")]
        public string? Description { get; set; }

        [StringLength(50, ErrorMessage = "Group maksimal 50 karakter")]
        public string? Group { get; set; }
    }

    public class UpdatePermissionDto
    {
        [StringLength(100, ErrorMessage = "Permission name maksimal 100 karakter")]
        public string? PermissionName { get; set; }

        [StringLength(255, ErrorMessage = "Description maksimal 255 karakter")]
        public string? Description { get; set; }

        [StringLength(50, ErrorMessage = "Group maksimal 50 karakter")]
        public string? Group { get; set; }
    }

    public class UpdateRolePermissionsDto
    {
        [Required(ErrorMessage = "Permission IDs wajib diisi")]
        public List<int> PermissionIds { get; set; } = new();
    }
}