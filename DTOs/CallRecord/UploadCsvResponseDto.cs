namespace Pm.DTOs.CallRecord
{
    public class UploadCsvResponseDto
    {
        public int TotalRecords { get; set; }
        public int SuccessfulRecords { get; set; }
        public int FailedRecords { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime UploadTime { get; set; } = DateTime.UtcNow;
    }
}