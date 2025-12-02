using Pm.DTOs.Common;

namespace Pm.DTOs.CallRecord
{
    public class CallRecordQueryDto : BaseQueryDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? CallCloseReason { get; set; }
        public int? HourGroup { get; set; }
    }
}