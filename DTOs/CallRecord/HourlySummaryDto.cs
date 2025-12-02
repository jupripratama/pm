namespace Pm.DTOs.CallRecord
{
    public class HourlySummaryDto
    {
        public DateTime Date { get; set; }
        public int HourGroup { get; set; }
        public string TimeRange { get; set; } = "";
        public int Qty { get; set; }
        public int TEBusy { get; set; }
        public decimal TEBusyPercent { get; set; }
        public int SysBusy { get; set; }
        public decimal SysBusyPercent { get; set; }
        public int Others { get; set; }
        public decimal OthersPercent { get; set; }
        
        // ✅ TAMBAHAN (optional)
        public string TEBusyDescription { get; set; } = "TE Busy";
        public string SysBusyDescription { get; set; } = "System Busy";
        public string OthersDescription { get; set; } = "Others";
    }
}
