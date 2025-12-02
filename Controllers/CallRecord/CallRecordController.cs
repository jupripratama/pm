using Microsoft.AspNetCore.Mvc;
using Pm.DTOs.CallRecord;
using Pm.Services;
using Pm.Helper;
using Microsoft.AspNetCore.Authorization;

namespace Pm.Controllers
{
    [Route("api/call-records")]
    [ApiController]
    [Produces("application/json")]
    public class CallRecordController : ControllerBase
    {
        private readonly ICallRecordService _callRecordService;
        private readonly IExcelExportService _excelExportService;
        private readonly ILogger<CallRecordController> _logger;

        public CallRecordController(
            ICallRecordService callRecordService,
            IExcelExportService excelExportService, 
            ILogger<CallRecordController> logger)
        {
            _callRecordService = callRecordService;
            _excelExportService = excelExportService;
            _logger = logger;
        }

        /// <summary>
        /// Upload dan import file CSV call records
        /// </summary>
        [Authorize(Policy = "CanImportCallRecords")]
        [DisableRequestSizeLimit]
        [HttpPost("import-csv")]
        public async Task<IActionResult> ImportCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return ApiResponse.BadRequest("file", "File tidak boleh kosong");

            if (file.Length > 100 * 1024 * 1024)
                return ApiResponse.BadRequest("file", "Ukuran file maksimal 100MB");

            var allowedExtensions = new[] { ".csv", ".txt" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
                return ApiResponse.BadRequest("file", "Hanya file CSV dan TXT yang diizinkan");

            var isAlreadyImported = await _callRecordService.IsFileAlreadyImported(file.FileName);
            if (isAlreadyImported)
            {
                return ApiResponse.BadRequest("file", $"File '{file.FileName}' sudah pernah diimport sebelumnya");
            }

            var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Starting CSV import for file: {FileName} ({Size} bytes)",
                    file.FileName, file.Length);

                using var stream = file.OpenReadStream();
                var result = await _callRecordService.ImportCsvAsync(stream, file.FileName);

                totalStopwatch.Stop();

                var message = result.SuccessfulRecords > 0
                    ? $"Import berhasil. {result.SuccessfulRecords:N0} record berhasil diproses dalam {totalStopwatch.ElapsedMilliseconds}ms"
                    : "Import gagal - tidak ada record yang berhasil diproses";

                if (result.FailedRecords > 0)
                    message += $", {result.FailedRecords:N0} record gagal";

                if (result.Errors.Any())
                    message += $". Errors: {string.Join("; ", result.Errors)}";

                return ApiResponse.Success(
                data: new { 
                    records = result, // ganti nama biar jelas
                    totalTimeMs = totalStopwatch.ElapsedMilliseconds 
                },
                message: message
            );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing CSV file {FileName}", file.FileName);
                return ApiResponse.InternalServerError($"Terjadi kesalahan saat mengimport file: {ex.Message}");
            }
        }

        /// <summary>
        /// Get call records dengan pagination dan filtering
        /// </summary>
        [Authorize(Policy = "CanViewCallRecords")]
        [HttpGet]
       public async Task<IActionResult> GetCallRecords([FromQuery] CallRecordQueryDto query)
        {
            try
            {
                var result = await _callRecordService.GetCallRecordsAsync(query);
                return ApiResponse.Success(result, "Data call records berhasil dimuat");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting call records");
                return ApiResponse.InternalServerError($"Terjadi kesalahan saat mengambil data call records: {ex.Message}");
            }
        }

        /// <summary>
        /// Get daily summary untuk tanggal tertentu
        /// </summary>
        [Authorize(Policy = "CanViewDetailCallRecords")]
        [HttpGet("summary/daily/{date}")]
        public async Task<IActionResult> GetDailySummary([FromRoute] string date)
        {
            if (!DateTime.TryParse(date, out var parsedDate))
                return ApiResponse.BadRequest("date", "Format tanggal tidak valid. Gunakan format YYYY-MM-DD");

            try
            {
                var result = await _callRecordService.GetDailySummaryAsync(parsedDate);
                return ApiResponse.Success(result, $"Summary harian untuk tanggal {parsedDate:yyyy-MM-dd} berhasil dimuat");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily summary for {Date}", date);
                return ApiResponse.InternalServerError($"Terjadi kesalahan saat mengambil summary harian: {ex.Message}");
            }
        }

        /// <summary>
        /// Get overall summary dengan semua jenis average calculations
        /// </summary>
        [Authorize(Policy = "CanViewDetailCallRecords")]
        [HttpGet("summary/overall")]
        public async Task<IActionResult> GetOverallSummary(
            [FromQuery] string startDate, 
            [FromQuery] string endDate)
        {
            if (!DateTime.TryParse(startDate, out var parsedStartDate))
                return ApiResponse.BadRequest("startDate", "Format startDate tidak valid. Gunakan format YYYY-MM-DD");

            if (!DateTime.TryParse(endDate, out var parsedEndDate))
                return ApiResponse.BadRequest("endDate", "Format endDate tidak valid. Gunakan format YYYY-MM-DD");

            if (parsedStartDate > parsedEndDate)
                return ApiResponse.BadRequest("date", "StartDate tidak boleh lebih besar dari endDate");

            if ((parsedEndDate - parsedStartDate).Days > 90)
                return ApiResponse.BadRequest("date", "Rentang tanggal maksimal 90 hari");

            try
            {
                var result = await _callRecordService.GetOverallSummaryAsync(parsedStartDate, parsedEndDate);
                return ApiResponse.Success(result, $"Overall summary dari {parsedStartDate:yyyy-MM-dd} sampai {parsedEndDate:yyyy-MM-dd} berhasil dimuat");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting overall summary from {StartDate} to {EndDate}", 
                    startDate, endDate);
                return ApiResponse.InternalServerError($"Terjadi kesalahan saat mengambil overall summary: {ex.Message}");
            }
        }

        [Authorize(Policy = "CanExportCallRecordsExcel")]
        [HttpGet("export/daily-summary/{date}")]
        public async Task<IActionResult> ExportDailySummaryToExcel([FromRoute] string date)
        {
            if (!DateTime.TryParse(date, out var parsedDate))
                return ApiResponse.BadRequest("date", "Format tanggal tidak valid. Gunakan format YYYY-MM-DD");

            try
            {
                var summary = await _callRecordService.GetDailySummaryAsync(parsedDate);
                var excelBytes = await _excelExportService.ExportDailySummaryToExcelAsync(parsedDate, summary);

                var fileName = $"Daily_Summary_{parsedDate:yyyy-MM-dd}.xlsx";
                
                return File(excelBytes, 
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting daily summary to Excel");
                return ApiResponse.InternalServerError($"Terjadi kesalahan saat export Excel: {ex.Message}");
            }
        }

        [Authorize(Policy = "CanExportCallRecordsExcel")]
        [HttpGet("export/overall-summary")]
        public async Task<IActionResult> ExportOverallSummaryToExcel(
            [FromQuery] string startDate,
            [FromQuery] string endDate)
        {
            if (!DateTime.TryParse(startDate, out var parsedStartDate))
                return ApiResponse.BadRequest("startDate", "Format startDate tidak valid. Gunakan format YYYY-MM-DD");

            if (!DateTime.TryParse(endDate, out var parsedEndDate))
                return ApiResponse.BadRequest("endDate", "Format endDate tidak valid. Gunakan format YYYY-MM-DD");

            if (parsedStartDate > parsedEndDate)
                return ApiResponse.BadRequest("date", "StartDate tidak boleh lebih besar dari endDate");

            if ((parsedEndDate - parsedStartDate).Days > 90)
                return ApiResponse.BadRequest("date", "Rentang tanggal maksimal 90 hari");

            try
            {
                var summary = await _callRecordService.GetOverallSummaryAsync(parsedStartDate, parsedEndDate);
                var excelBytes = await _excelExportService.ExportMultipleDailySummariesToExcelAsync(
                    parsedStartDate, parsedEndDate, summary);

                var fileName = $"Daily_Summary_{parsedStartDate:yyyy-MM-dd}_to_{parsedEndDate:yyyy-MM-dd}.xlsx";

                return File(excelBytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting overall summary to Excel");
                return ApiResponse.InternalServerError($"Terjadi kesalahan saat export Excel: {ex.Message}");
            }
        }

        /// <summary>
        /// Download call records sebagai CSV file
        /// </summary>
        [Authorize(Policy = "CanExportCallRecordsCsv")]
        [HttpGet("export/csv")]
        public async Task<IActionResult> ExportCallRecordsToCsv(
            [FromQuery] string startDate,
            [FromQuery] string endDate)
        {
            if (!DateTime.TryParse(startDate, out var parsedStartDate))
                return ApiResponse.BadRequest("startDate", "Format startDate tidak valid. Gunakan format YYYY-MM-DD");

            if (!DateTime.TryParse(endDate, out var parsedEndDate))
                return ApiResponse.BadRequest("endDate", "Format endDate tidak valid. Gunakan format YYYY-MM-DD");

            if (parsedStartDate > parsedEndDate)
                return ApiResponse.BadRequest("date", "StartDate tidak boleh lebih besar dari endDate");

            if ((parsedEndDate - parsedStartDate).Days > 90)
                return ApiResponse.BadRequest("date", "Rentang tanggal maksimal 90 hari");

            try
            {
                var csvBytes = await _callRecordService.ExportCallRecordsToCsvAsync(parsedStartDate, parsedEndDate);
                var fileName = $"CallRecords_{parsedStartDate:yyyyMMdd}_to_{parsedEndDate:yyyyMMdd}.csv";

                return File(csvBytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting call records to CSV");
                return ApiResponse.InternalServerError($"Terjadi kesalahan saat export CSV: {ex.Message}");
            }
        }

        /// <summary>
        /// Download call records untuk tanggal tertentu sebagai CSV
        /// </summary>
        [Authorize(Policy = "CanExportCallRecordsCsv")]
        [HttpGet("export/csv/{date}")]
        public async Task<IActionResult> ExportDailyCallRecordsToCsv([FromRoute] string date)
        {
            if (!DateTime.TryParse(date, out var parsedDate))
                return ApiResponse.BadRequest("date", "Format tanggal tidak valid. Gunakan format YYYY-MM-DD");

            try
            {
                var csvBytes = await _callRecordService.ExportCallRecordsToCsvAsync(parsedDate, parsedDate);
                var fileName = $"CallRecords_{parsedDate:yyyyMMdd}.csv";

                return File(csvBytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting daily call records to CSV");
                return ApiResponse.InternalServerError($"Terjadi kesalahan saat export CSV: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete call records untuk tanggal tertentu
        /// </summary>
        [Authorize(Policy = "CanDeleteCallRecords")]
        [HttpDelete("{date}")]
        public async Task<IActionResult> DeleteCallRecords([FromRoute] string date)
        {
            if (!DateTime.TryParse(date, out var parsedDate))
                return ApiResponse.BadRequest("date", "Format tanggal tidak valid. Gunakan format YYYY-MM-DD");

            try
            {
                var success = await _callRecordService.DeleteCallRecordsAsync(parsedDate);

                if (success)
                {
                    return ApiResponse.Success(
                        data: new { deleted = true },
                        message: $"Call records untuk tanggal {parsedDate:yyyy-MM-dd} berhasil dihapus"
                    );
                }
                else
                {
                    return ApiResponse.InternalServerError("Gagal menghapus call records");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting call records");
                return ApiResponse.InternalServerError($"Terjadi kesalahan saat menghapus call records: {ex.Message}");
            }
        }

        /// <summary>
        /// Reset semua data call records dan summaries (DANGER!)
        /// </summary>
        [Authorize(Policy = "CanDeleteAllData")]
        [HttpDelete("reset-all")]
        public async Task<IActionResult> ResetAllData([FromQuery] string confirmation)
        {
            if (confirmation != "DELETE_ALL_DATA")
            {
                return ApiResponse.BadRequest("confirmation", "Konfirmasi tidak valid. Gunakan query parameter: ?confirmation=DELETE_ALL_DATA");
            }

            try
            {
                _logger.LogWarning("⚠️ RESET DATABASE - Deleting all call records and summaries");
                await _callRecordService.ResetAllDataAsync();

                return ApiResponse.Success(
                    data: new {
                        warning = "Semua data telah dihapus permanent"
                    },
                    message: "Semua data call records dan summaries berhasil dihapus"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting database");
                return ApiResponse.InternalServerError($"Gagal reset database: {ex.Message}");
            }
        }

        /// <summary>
        /// Endpoint untuk mendapatkan Fleet Statistics
        /// </summary>
        [Authorize(Policy = "CanViewCallRecords")]
        [HttpGet("fleet-statistics")]
        [ProducesResponseType(typeof(FleetStatisticsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetFleetStatistics(
            [FromQuery] DateTime? date = null,
            [FromQuery] int top = 10,
            [FromQuery] FleetStatisticType? type = null)
        {
            try
            {
                var targetDate = date ?? DateTime.Today;
                var selectedType = type ?? FleetStatisticType.All;
                
                if (top < 1 || top > 100)
                {
                    return ApiResponse.BadRequest("top", "Parameter 'top' harus antara 1 dan 100");
                }

                var stats = await _callRecordService.GetFleetStatisticsAsync(targetDate, top, selectedType);
                
                if (stats.TopCallers.Count == 0 && stats.TopCalledFleets.Count == 0)
                {
                    return ApiResponse.NotFound($"Tidak ada data fleet statistics untuk tanggal {targetDate:yyyy-MM-dd}");
                }

                var message = selectedType switch
                {
                    FleetStatisticType.Caller => "Top Caller Fleets berhasil dimuat",
                    FleetStatisticType.Called => "Top Called Fleets berhasil dimuat",
                    _ => "Fleet statistics berhasil dimuat"
                };

                return ApiResponse.Success(stats, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fleet statistics");
                return ApiResponse.BadRequest("message", ex.Message);
            }
        }
    }
}