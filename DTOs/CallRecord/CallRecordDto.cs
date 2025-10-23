using System.Text.Json.Serialization;

namespace Pm.DTOs.CallRecord
{
    public class CallRecordDto
    {
        public int CallRecordId { get; set; }
        [JsonIgnore] // Ignore property asli
        public DateTime CallDate { get; set; }
        public TimeSpan CallTime { get; set; }
        public int CallCloseReason { get; set; }
        public string CloseReasonDescription { get; set; } = string.Empty;
        public int HourGroup { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}