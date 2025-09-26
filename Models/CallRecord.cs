using System.ComponentModel.DataAnnotations;

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
        
        public int HourGroup => CallTime.Hours;
        
        public string CloseReasonDescription => CallCloseReason switch
        {
            0 => "TE Busy",
            1 => "Sys Busy",
            _ => "Others"
        };
    }
}