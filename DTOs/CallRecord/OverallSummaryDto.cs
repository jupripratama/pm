namespace Pm.DTOs.CallRecord
{
    public class OverallSummaryDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalDays { get; set; }
        public List<DailySummaryDto> DailyData { get; set; } = new();
        
        // Overall totals
        public int TotalCalls { get; set; }
        public int TotalTEBusy { get; set; }
        public int TotalSysBusy { get; set; }
        public int TotalOthers { get; set; }
        
        // Total average percentages (dari keseluruhan data)
        public decimal TotalAvgTEBusyPercent { get; set; }
        public decimal TotalAvgSysBusyPercent { get; set; }
        public decimal TotalAvgOthersPercent { get; set; }
        
        // Average per day (rata-rata jumlah per hari)
        public decimal AvgCallsPerDay { get; set; }
        public decimal AvgTEBusyPerDay { get; set; }
        public decimal AvgSysBusyPerDay { get; set; }
        public decimal AvgOthersPerDay { get; set; }
        
        // Daily average percentages (rata-rata dari persentase harian)
        public decimal DailyAvgTEBusyPercent { get; set; }
        public decimal DailyAvgSysBusyPercent { get; set; }
        public decimal DailyAvgOthersPercent { get; set; }
    }
}