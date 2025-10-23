namespace Pm.DTOs.CallRecord
{
    public class FleetStatisticsDto
    {
       public DateTime Date { get; set; }
        public List<TopCallerFleetDto> TopCallers { get; set; } = new();
        public List<TopCalledFleetDto> TopCalledFleets { get; set; } = new();
        public int TotalCallsInDay { get; set; }
        public int TotalDurationInDaySeconds { get; set; }
        public string TotalDurationInDayFormatted { get; set; } = string.Empty;
        public int TotalUniqueCallers { get; set; }
        public int TotalUniqueCalledFleets { get; set; }
    }
}