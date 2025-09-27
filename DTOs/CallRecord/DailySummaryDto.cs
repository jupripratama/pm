namespace Pm.DTOs.CallRecord
{
    public class DailySummaryDto
    {
        public DateTime Date { get; set; }
        public List<HourlySummaryDto> HourlyData { get; set; } = new();
        
        // Daily totals
        public int TotalQty { get; set; }
        public int TotalTEBusy { get; set; }
        public int TotalSysBusy { get; set; }
        public int TotalOthers { get; set; }
        
        // Daily average percentages
        public decimal AvgTEBusyPercent { get; set; }
        public decimal AvgSysBusyPercent { get; set; }
        public decimal AvgOthersPercent { get; set; }
    }
}