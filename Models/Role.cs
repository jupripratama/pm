using System.ComponentModel.DataAnnotations;

namespace Pm.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }
        
        [Required, MaxLength(50)]
        public string RoleName { get; set; } = string.Empty;
        
        [MaxLength(255)]
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
