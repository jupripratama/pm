using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    public class FleetStatistic
    {
        [Key]
        public int FleetStatisticId { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime CallDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string CallerFleet { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string CalledFleet { get; set; } = string.Empty;

        public int CallCount { get; set; } = 1;
        
        public int TotalDuration { get; set; } // dalam detik
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}