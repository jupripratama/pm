using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pm.Models
{
    public class CallSummary
    {
        [Key]
        public int CallSummaryId { get; set; }

        [Required]
        [Column(TypeName = "date")]
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

        // ✅ TAMBAHAN: Method untuk mendapatkan deskripsi breakdown
        public string GetTEBusyDescription() => "TE Busy - The called terminal equipment is already in a call";
        
        public string GetSysBusyDescription() => "System Busy - The network is overloaded or has problems";
        
        public string GetOthersDescription() => "Others - No Answer, Not Found, Complete, Preempted, Timeout, Inactive, Callback, Unsupported Request, Invalid Call";

        // ✅ Atau jika mau dalam satu method:
        public string GetCloseReasonDescription(int closeReason) => closeReason switch
        {
            0 => "TE Busy - The called terminal equipment is already in a call",
            1 => "System Busy - The network is overloaded or has problems",
            2 => "No Answer - The called party does not answer",
            3 => "Not Found - The ident of the called party is valid but it is either not registered or the node could not route the call",
            4 => "Complete - The call was completed",
            5 => "Preempted - The call was cleared down to make a channel available for a priority or emergency call",
            6 => "Timeout - The call exceeded the current maximum call duration or the maximum allowable call setup time",
            7 => "Inactive - One or more of the parties was inactive. The inactivity timer expired",
            8 => "Callback - The call to a line dispatcher terminal was put in the callback queue",
            9 => "Unsupported Request - The call could not be processed because the system does not support it",
            10 => "Invalid Call - The call failed the node's validation check",
            _ => "Others - Unknown reason"
        };
    }
}