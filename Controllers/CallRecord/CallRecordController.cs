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

        public CallRecordController(ICallRecordService callRecordService,IExcelExportService excelExportService, ILogger<CallRecordController> logger)
        {
            _callRecordService = callRecordService;
            _excelExportService = excelExportService;
            _logger = logger;
        }

        private static readonly Dictionary<string, ImportProgress> _importProgress = new();

        public class ImportProgress
        {
            public string FileName { get; set; } = "";
            public int TotalRows { get; set; }
            public int ProcessedRows { get; set; }
            public int SuccessfulRows { get; set; }
            public int FailedRows { get; set; }
            public DateTime StartTime { get; set; }
            public bool IsCompleted { get; set; }
        }



        /// <summary>
        /// Upload dan import file CSV call records
        /// </summary>
        [Authorize(Policy = "CanImportCallRecords")]
        [HttpPost("import-csv")]
        public async Task<IActionResult> ImportCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "File tidak boleh kosong" });

            if (file.Length > 100 * 1024 * 1024) // 100MB max
                return BadRequest(new { message = "Ukuran file maksimal 100MB" });

            var allowedExtensions = new[] { ".csv", ".txt" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest(new { message = "Hanya file CSV dan TXT yang diizinkan" });

            // ✅ CEK APAKAH FILE SUDAH PERNAH DI IMPORT
            var isAlreadyImported = await _callRecordService.IsFileAlreadyImported(file.FileName);
            if (isAlreadyImported)
            {
                return BadRequest(new { message = $"File '{file.FileName}' sudah pernah diimport sebelumnya" });
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

                return Ok(new
                {
                    message,
                    data = result,
                    totalTimeMs = totalStopwatch.ElapsedMilliseconds
                });
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

                // Simpan message di HttpContext.Items untuk ResponseWrapperFilter
                HttpContext.Items["message"] = "Data call records berhasil dimuat";

                return Ok(new { data = result });
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

                HttpContext.Items["message"] = $"Summary harian untuk tanggal {parsedDate:yyyy-MM-dd} berhasil dimuat";

                return Ok(new { data = result });
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
                
                HttpContext.Items["message"] = $"Overall summary dari {parsedStartDate:yyyy-MM-dd} sampai {parsedEndDate:yyyy-MM-dd} berhasil dimuat";
                
                return Ok(new { data = result });
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

        [Authorize(Policy = "CanExportCallRecordsExcel")] // Tambahkan authorize jika perlu
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

                // ✅ GUNAKAN METHOD BARU UNTUK MULTIPLE SHEETS
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
        /// <param name="startDate">Tanggal mulai (YYYY-MM-DD)</param>
        /// <param name="endDate">Tanggal akhir (YYYY-MM-DD)</param>
        /// <returns>CSV file download</returns>
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

                // Untuk file download, return langsung FileResult (tidak melalui wrapper)
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
        /// <param name="date">Tanggal (YYYY-MM-DD)</param>
        /// <returns>CSV file download</returns>
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

                // Untuk file download, return langsung FileResult
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
                    HttpContext.Items["message"] = $"Call records untuk tanggal {parsedDate:yyyy-MM-dd} berhasil dihapus";
                    return Ok(new { data = new { deleted = true } });
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
        
        // Di CallRecordController.cs

        /// <summary>
        /// Reset semua data call records dan summaries (DANGER!)
        /// </summary>
        [Authorize(Policy = "CanDeleteAllData")] // Pastikan hanya admin yang bisa akses
        [HttpDelete("reset-all")]
        public async Task<IActionResult> ResetAllData([FromQuery] string confirmation)
        {
            // Safety check
            if (confirmation != "DELETE_ALL_DATA")
            {
                return BadRequest(new { 
                    message = "Konfirmasi tidak valid. Gunakan query parameter: ?confirmation=DELETE_ALL_DATA" 
                });
            }

            try
            {
                _logger.LogWarning("⚠️ RESET DATABASE - Deleting all call records and summaries");
                
                await _callRecordService.ResetAllDataAsync();
                
                HttpContext.Items["message"] = "Semua data call records dan summaries berhasil dihapus";
                return Ok(new { 
                    message = "Database berhasil direset",
                    warning = "Semua data telah dihapus permanent"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting database");
                return ApiResponse.InternalServerError($"Gagal reset database: {ex.Message}");
            }
        }
    }
}