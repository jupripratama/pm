using System.ComponentModel.DataAnnotations;

namespace Pm.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? PhotoUrl { get; set; } // Added for profile photo

        public bool IsActive { get; set; } = true;

        public int RoleId { get; set; }
        public virtual Role? Role { get; set; }

        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}