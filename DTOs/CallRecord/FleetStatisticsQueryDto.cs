namespace Pm.DTOs.CallRecord
{
     public class FleetStatisticsQueryDto
    {
        public DateTime Date { get; set; } = DateTime.Today;
        public int Top { get; set; } = 10; // default top 10
        public FleetStatisticType? Type { get; set; } // dropdown: Both, Caller, Called
    }
    
    public enum FleetStatisticType
    {
        All,
        Caller ,
        Called 
    }
}