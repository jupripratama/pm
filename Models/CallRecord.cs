using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    public class CallRecord
    {
        [Key]
        public int CallRecordId { get; set; }
        
        [Required]
        public DateTime CallDate { get; set; }
        
        [Required]
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
            1 => "Sys Busy",
            _ => "Others"
        };
    }
}