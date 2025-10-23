namespace Pm.DTOs.CallRecord
{
   public class TopCallerFleetDto
    {
        public int Rank { get; set; }
        public string CallerFleet { get; set; } = string.Empty;
        public int TotalCalls { get; set; }
        public int TotalDurationSeconds { get; set; }
        public string TotalDurationFormatted { get; set; } = string.Empty;
        public decimal AverageDurationSeconds { get; set; }
        public string AverageDurationFormatted { get; set; } = string.Empty;
    }
}