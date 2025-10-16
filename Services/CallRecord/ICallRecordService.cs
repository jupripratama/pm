using Pm.DTOs.CallRecord;
using Pm.DTOs.Common;

namespace Pm.Services
{
    public interface ICallRecordService
    {
        Task<UploadCsvResponseDto> ImportCsvAsync(Stream csvStream, string fileName);
        Task<byte[]> ExportCallRecordsToCsvAsync(DateTime startDate, DateTime endDate);
        Task<PagedResultDto<CallRecordDto>> GetCallRecordsAsync(CallRecordQueryDto query);
        Task<DailySummaryDto> GetDailySummaryAsync(DateTime date);
        Task<OverallSummaryDto> GetOverallSummaryAsync(DateTime startDate, DateTime endDate);
        Task<List<HourlySummaryDto>> GetHourlySummaryAsync(DateTime date);
        Task<bool> RegenerateSummariesAsync(DateTime startDate, DateTime endDate);
        Task<bool> DeleteCallRecordsAsync(DateTime date);
    }
}
