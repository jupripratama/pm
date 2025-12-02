using System.ComponentModel.DataAnnotations;

namespace Pm.Models
{
    public class ActivityLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Module { get; set; } = string.Empty; // misal: InspeksiTemuanKpc

        public int? EntityId { get; set; } // ID temuan

        [Required]
        public string Action { get; set; } = string.Empty; // Create, Update, Delete

        public int UserId { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        // Relasi
        public virtual User User { get; set; } = null!;
    }
}