using System.ComponentModel.DataAnnotations;

namespace Pm.Models
{
    public class CallSummary
    {
        [Key]
        public int CallSummaryId { get; set; }
        
        [Required]
        public DateTime SummaryDate { get; set; }
        
        [Required]
        public int HourGroup { get; set; } // 0-23
        
        public int TotalQty { get; set; }
        public int TEBusyCount { get; set; }
        public int SysBusyCount { get; set; }
        public int OthersCount { get; set; }
        
        public decimal TEBusyPercent { get; set; }
        public decimal SysBusyPercent { get; set; }
        public decimal OthersPercent { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        public string TimeRange => $"{HourGroup:00}.00 - {HourGroup:00}.59";
    }
}