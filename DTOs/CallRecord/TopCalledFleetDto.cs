namespace Pm.DTOs.CallRecord
{
    public class TopCalledFleetDto
    {
        public int Rank { get; set; }
        public string CalledFleet { get; set; } = string.Empty;
        public int TotalCalls { get; set; }
        public int TotalDurationSeconds { get; set; }
        public string TotalDurationFormatted { get; set; } = string.Empty;
        public decimal AverageDurationSeconds { get; set; }
        public string AverageDurationFormatted { get; set; } = string.Empty;
        public int UniqueCallers { get; set; }
    }
}