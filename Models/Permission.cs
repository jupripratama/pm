using System.ComponentModel.DataAnnotations;

namespace Pm.Models
{
    public class Permission
    {
        [Key]
        public int PermissionId { get; set; }
        
        [Required, MaxLength(100)]
        public string PermissionName { get; set; } = string.Empty;
        
        [MaxLength(255)]
        public string? Description { get; set; }
        
        [MaxLength(50)]
        public string? Group { get; set; } // Untuk mengelompokkan permission (user, role, etc)
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}