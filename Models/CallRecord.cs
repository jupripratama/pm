using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    public class CallRecord
    {
        [Key]
        public int CallRecordId { get; set; }

        [Required]
        [Column(TypeName = "date")]
        public DateTime CallDate { get; set; }

        [Required]
        [Column(TypeName = "time(0)")]
        public TimeSpan CallTime { get; set; }
        
        [Required]
        public int CallCloseReason { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
       // Method untuk mendapatkan HourGroup (bukan property)
        public int GetHourGroup() => CallTime.Hours;
        
        // Method untuk mendapatkan description (bukan property)
        public string GetCloseReasonDescription() => CallCloseReason switch
        {
            0 => "TE Busy",
            1 => "System Busy",
            2 => "No Answer",
            3 => "Not Found",
            4 => "Complete",
            5 => "Preempted",
            6 => "Timeout",
            7 => "Inactive",
            8 => "Callback",
            9 => "Unsupported Request",
            10 => "Invalid Call",
            _ => $"Unknown ({CallCloseReason})"
        };
    }
}