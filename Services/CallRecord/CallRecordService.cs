using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Pm.Data;
using Pm.DTOs.CallRecord;
using Pm.DTOs.Common;
using Pm.Models;
using System.Globalization;
using System.Text;

namespace Pm.Services
{
    public class CallRecordService : ICallRecordService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CallRecordService> _logger;

        public CallRecordService(AppDbContext context, ILogger<CallRecordService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UploadCsvResponseDto> ImportCsvAsync(Stream csvStream, string fileName)
        {
            var response = new UploadCsvResponseDto();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // ✅ CEK APAKAH FILE SUDAH PERNAH DI IMPORT
                if (await IsFileAlreadyImported(fileName))
                {
                    response.Errors.Add($"File '{fileName}' sudah pernah diimport sebelumnya");
                    _logger.LogWarning("❌ File already imported: {FileName}", fileName);
                    return response;
                }
            
                // BACA SEMUA ISI FILE
                using var reader = new StreamReader(csvStream, Encoding.UTF8);
                var allContent = await reader.ReadToEndAsync();
                var lines = allContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                _logger.LogInformation("🚀 Starting CSV import: {Count} lines from {FileName}", lines.Length, fileName);

                var parseStart = stopwatch.ElapsedMilliseconds;
                
                // Parallel parsing untuk performa maksimal
                var records = lines
                    .AsParallel()
                    .WithDegreeOfParallelism(Environment.ProcessorCount)
                    .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                    .Select((line, idx) => ParseCsvRowOptimized(line, idx + 1))
                    .Where(r => r != null)
                    .Cast<CallRecord>()
                    .ToList();

                _logger.LogInformation("✅ Parsed {Successful}/{Total} records in {Ms}ms", 
                    records.Count, lines.Length, stopwatch.ElapsedMilliseconds - parseStart);

                if (records.Any())
                {
                    var insertStart = stopwatch.ElapsedMilliseconds;
                    await BulkInsertOptimizedAsync(records);
                    _logger.LogInformation("💾 Inserted {Count} records in {Ms}ms", 
                        records.Count, stopwatch.ElapsedMilliseconds - insertStart);
                }

                response.SuccessfulRecords = records.Count;
                response.TotalRecords = lines.Length;
                response.FailedRecords = lines.Length - records.Count;

                stopwatch.Stop();
                _logger.LogInformation("🎉 Import completed in {Ms}ms - Success: {Success}, Failed: {Failed}",
                    stopwatch.ElapsedMilliseconds, response.SuccessfulRecords, response.FailedRecords);
                    
                    // ✅ SIMPAN HISTORY IMPORT JIKA SUKSES
                if (records.Any())
                {
                    var importHistory = new FileImportHistory
                    {
                        FileName = fileName,
                        ImportDate = DateTime.UtcNow,
                        RecordCount = records.Count,
                        Status = "Completed"
                    };
                    
                    await _context.FileImportHistories.AddAsync(importHistory);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("📝 Import history saved for file: {FileName}", fileName);
                }

                // Generate summaries async (fire and forget)
                _ = Task.Run(async () =>
                {
                    try 
                    {
                        var dates = records.Select(r => r.CallDate.Date).Distinct();
                        foreach (var date in dates)
                        {
                            await GenerateDailySummaryAsync(date);
                        }
                        _logger.LogInformation("📊 Summaries generated for {Count} dates", dates.Count());
                    }
                    catch (Exception ex) 
                    { 
                        _logger.LogError(ex, "Summary generation error"); 
                    }
                });

                return response;
            }
            catch (Exception ex)
            {
                // ✅ SIMPAN HISTORY IMPORT JIKA GAGAL
                var failedHistory = new FileImportHistory
                {
                    FileName = fileName,
                    ImportDate = DateTime.UtcNow,
                    RecordCount = 0,
                    Status = "Failed",
                    ErrorMessage = ex.Message
                };
                
                await _context.FileImportHistories.AddAsync(failedHistory);
                await _context.SaveChangesAsync();
            
                _logger.LogError(ex, "💥 Import error for {FileName}", fileName);
                response.Errors.Add($"Import error: {ex.Message}");
                return response;
            }
        }

        private CallRecord? ParseCsvRowOptimized(string line, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(line)) 
                return null;
            
            try
            {
                // Remove surrounding quotes jika ada
                if (line.StartsWith('"') && line.EndsWith('"'))
                    line = line.Substring(1, line.Length - 2);

                var parts = line.Split(',');
                if (parts.Length < 3) 
                    return null;

                // Parse date dari kolom pertama
                var dateStr = parts[0].Trim();
                if (dateStr.Length != 8 || !int.TryParse(dateStr, out int dateInt))
                    return null;
                    
                var year = dateInt / 10000;
                var month = (dateInt / 100) % 100;
                var day = dateInt % 100;
                
                if (year < 2000 || year > 2100 || month < 1 || month > 12 || day < 1 || day > 31)
                    return null;
                    
                var callDate = new DateTime(year, month, day);

                // Parse time dari kolom kedua
                if (!TimeSpan.TryParse(parts[1].Trim(), out var callTime))
                    return null;

                // Parse close reason dari kolom kedua terakhir
                var reasonStr = parts[parts.Length - 2].Trim();
                if (!int.TryParse(reasonStr, out int callCloseReason))
                    return null;

                return new CallRecord
                {
                    CallDate = callDate,
                    CallTime = callTime,
                    CallCloseReason = callCloseReason,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch
            {
                return null;
            }
        }

        private async Task BulkInsertOptimizedAsync(List<CallRecord> records)
        {
            if (!records.Any()) return;

            const int batchSize = 10000;
            var totalBatches = (int)Math.Ceiling((double)records.Count / batchSize);

            _logger.LogInformation("📦 Inserting {TotalRecords} records in {BatchCount} batches sequentially",
                records.Count, totalBatches);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = records.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                await InsertBatchSequentialAsync(batch, batchIndex);

                // Small delay antara batches untuk mengurangi load database
                if (batchIndex < totalBatches - 1)
                    await Task.Delay(5);
            }
        }

        private async Task InsertBatchSequentialAsync(List<CallRecord> batch, int batchIndex)
        {
            try
            {
                var values = new StringBuilder();
                var parameters = new List<object>();
                
                for (int i = 0; i < batch.Count; i++)
                {
                    var r = batch[i];
                    if (i > 0) values.Append(",");
                    
                    var baseIndex = i * 4;
                    // YANG INI - sederhana tanpa casting
                    values.Append($"(@p{baseIndex},@p{baseIndex+1},@p{baseIndex+2},@p{baseIndex+3})");
                    
                    parameters.Add(r.CallDate);
                    parameters.Add(r.CallTime);
                    parameters.Add(r.CallCloseReason);
                    parameters.Add(r.CreatedAt);
                }
                
                var sql = $"INSERT INTO CallRecords (CallDate, CallTime, CallCloseReason, CreatedAt) VALUES {values}";
                await _context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray());
                
                _logger.LogInformation("✅ Batch {BatchIndex} inserted: {Count} records", batchIndex + 1, batch.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error inserting batch {BatchIndex}", batchIndex);
                throw;
            }
        }
        
        public async Task<byte[]> ExportCallRecordsToCsvAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Exporting call records from {StartDate} to {EndDate}", startDate, endDate);

                var records = await _context.CallRecords
                    .Where(cr => cr.CallDate >= startDate.Date && cr.CallDate <= endDate.Date)
                    .OrderBy(cr => cr.CallDate)
                    .ThenBy(cr => cr.CallTime)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} records for export", records.Count);

                var csv = new StringBuilder();
                csv.AppendLine("DATE;TIME;CALL CLOSE REASON");

                foreach (var record in records)
                {
                    try
                    {
                        // Format date dengan handling error
                        var date = record.CallDate.ToString("yyyyMMdd");
                        
                        // Format time dengan handling yang lebih robust
                        var time = "000000"; // default value
                        if (record.CallTime != null)
                        {
                            try
                            {
                                // Beberapa cara format TimeSpan yang aman
                                time = record.CallTime.ToString(@"hh\:mm\:ss").Replace(":", "");
                                // Atau alternatif:
                                // time = $"{(int)record.CallTime.TotalHours:D2}{record.CallTime.Minutes:D2}{record.CallTime.Seconds:D2}";
                            }
                            catch (FormatException fmtEx)
                            {
                                _logger.LogWarning(fmtEx, "Invalid time format for record {RecordId}, using default", record.CallRecordId);
                                time = "000000";
                            }
                        }

                        // Handle CallCloseReason yang null
                        var closeReason = record.CallCloseReason.ToString();

                        csv.AppendLine($"{date};{time};{closeReason}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error formatting record {RecordId} for CSV export", record.CallRecordId);
                        // Skip record yang error atau gunakan default values
                        var date = record.CallDate.ToString("yyyyMMdd");
                        var time = "000000";
                        var closeReason = record.CallCloseReason.ToString();
                        csv.AppendLine($"{date};{time};{closeReason}");
                    }
                }

                _logger.LogInformation("Successfully exported {Count} call records to CSV", records.Count);
                return Encoding.UTF8.GetBytes(csv.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting call records to CSV");
                throw new Exception($"Terjadi kesalahan saat export CSV: {ex.Message}", ex);
            }
        }

        public async Task<PagedResultDto<CallRecordDto>> GetCallRecordsAsync(CallRecordQueryDto query)
        {
            var dbQuery = _context.CallRecords.AsQueryable();

            // Apply filters
            if (query.StartDate.HasValue)
                dbQuery = dbQuery.Where(cr => cr.CallDate >= query.StartDate.Value.Date);

            if (query.EndDate.HasValue)
                dbQuery = dbQuery.Where(cr => cr.CallDate <= query.EndDate.Value.Date);

            if (query.CallCloseReason.HasValue)
                dbQuery = dbQuery.Where(cr => cr.CallCloseReason == query.CallCloseReason.Value);

            if (query.HourGroup.HasValue)
            {
                var targetHour = query.HourGroup.Value;
                dbQuery = dbQuery.Where(cr => cr.CallTime.Hours == targetHour);
            }

            if (!string.IsNullOrEmpty(query.Search))
            {
                if (int.TryParse(query.Search, out int searchReason))
                {
                    dbQuery = dbQuery.Where(cr => cr.CallCloseReason == searchReason);
                }
                else if (DateTime.TryParse(query.Search, out var searchDate))
                {
                    dbQuery = dbQuery.Where(cr => cr.CallDate.Date == searchDate.Date);
                }
            }

            // Apply sorting
            var sortDir = query.SortDir?.ToLower() ?? "desc";
            dbQuery = (query.SortBy?.ToLower()) switch
            {
                "calldate" => sortDir == "desc" 
                    ? dbQuery.OrderByDescending(cr => cr.CallDate) 
                    : dbQuery.OrderBy(cr => cr.CallDate),
                "calltime" => sortDir == "desc" 
                    ? dbQuery.OrderByDescending(cr => cr.CallTime) 
                    : dbQuery.OrderBy(cr => cr.CallTime),
                "callclosereason" => sortDir == "desc" 
                    ? dbQuery.OrderByDescending(cr => cr.CallCloseReason) 
                    : dbQuery.OrderBy(cr => cr.CallCloseReason),
                _ => dbQuery.OrderByDescending(cr => cr.CallDate).ThenByDescending(cr => cr.CallTime)
            };

            var total = await dbQuery.CountAsync();

            var callRecords = await dbQuery
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var dtos = callRecords.Select(ToDto).ToList();

            return new PagedResultDto<CallRecordDto>(dtos, query.Page, query.PageSize, total);
        }

        public async Task<DailySummaryDto> GetDailySummaryAsync(DateTime date)
        {
            var hourlyData = await GetHourlySummaryAsync(date);

            var dailySummary = new DailySummaryDto
            {
                Date = date.Date,
                HourlyData = hourlyData,
                TotalQty = hourlyData.Sum(h => h.Qty),
                TotalTEBusy = hourlyData.Sum(h => h.TEBusy),
                TotalSysBusy = hourlyData.Sum(h => h.SysBusy),
                TotalOthers = hourlyData.Sum(h => h.Others)
            };

            if (dailySummary.TotalQty > 0)
            {
                dailySummary.AvgTEBusyPercent = Math.Round((decimal)dailySummary.TotalTEBusy / dailySummary.TotalQty * 100, 2);
                dailySummary.AvgSysBusyPercent = Math.Round((decimal)dailySummary.TotalSysBusy / dailySummary.TotalQty * 100, 2);
                dailySummary.AvgOthersPercent = Math.Round((decimal)dailySummary.TotalOthers / dailySummary.TotalQty * 100, 2);
            }

            return dailySummary;
        }

        public async Task<List<HourlySummaryDto>> GetHourlySummaryAsync(DateTime date)
        {
            var summaries = await _context.CallSummaries
                .Where(cs => cs.SummaryDate.Date == date.Date)
                .OrderBy(cs => cs.HourGroup)
                .ToListAsync();

            if (!summaries.Any())
            {
                await GenerateDailySummaryAsync(date);
                summaries = await _context.CallSummaries
                    .Where(cs => cs.SummaryDate.Date == date.Date)
                    .OrderBy(cs => cs.HourGroup)
                    .ToListAsync();
            }

            return summaries.Select(s => new HourlySummaryDto
            {
                Date = s.SummaryDate,
                HourGroup = s.HourGroup,
                TimeRange = s.TimeRange,
                Qty = s.TotalQty,
                TEBusy = s.TEBusyCount,
                TEBusyPercent = s.TEBusyPercent,
                SysBusy = s.SysBusyCount,
                SysBusyPercent = s.SysBusyPercent,
                Others = s.OthersCount,
                OthersPercent = s.OthersPercent,
                 // ✅ TAMBAHAN
                TEBusyDescription = s.GetTEBusyDescription(),
                SysBusyDescription = s.GetSysBusyDescription(),
                OthersDescription = s.GetOthersDescription()
            }).ToList();
        }

        public async Task<OverallSummaryDto> GetOverallSummaryAsync(DateTime startDate, DateTime endDate)
        {
            var dailyData = new List<DailySummaryDto>();
            var currentDate = startDate.Date;

            while (currentDate <= endDate.Date)
            {
                var dailySummary = await GetDailySummaryAsync(currentDate);
                dailyData.Add(dailySummary);
                currentDate = currentDate.AddDays(1);
            }

            var totalDays = (endDate.Date - startDate.Date).Days + 1;

            var overallSummary = new OverallSummaryDto
            {
                StartDate = startDate.Date,
                EndDate = endDate.Date,
                TotalDays = totalDays,
                DailyData = dailyData,
                TotalCalls = dailyData.Sum(d => d.TotalQty),
                TotalTEBusy = dailyData.Sum(d => d.TotalTEBusy),
                TotalSysBusy = dailyData.Sum(d => d.TotalSysBusy),
                TotalOthers = dailyData.Sum(d => d.TotalOthers)
            };

            if (overallSummary.TotalCalls > 0)
            {
                overallSummary.TotalAvgTEBusyPercent = Math.Round((decimal)overallSummary.TotalTEBusy / overallSummary.TotalCalls * 100, 2);
                overallSummary.TotalAvgSysBusyPercent = Math.Round((decimal)overallSummary.TotalSysBusy / overallSummary.TotalCalls * 100, 2);
                overallSummary.TotalAvgOthersPercent = Math.Round((decimal)overallSummary.TotalOthers / overallSummary.TotalCalls * 100, 2);
            }

            if (totalDays > 0)
            {
                overallSummary.AvgCallsPerDay = Math.Round((decimal)overallSummary.TotalCalls / totalDays, 2);
                overallSummary.AvgTEBusyPerDay = Math.Round((decimal)overallSummary.TotalTEBusy / totalDays, 2);
                overallSummary.AvgSysBusyPerDay = Math.Round((decimal)overallSummary.TotalSysBusy / totalDays, 2);
                overallSummary.AvgOthersPerDay = Math.Round((decimal)overallSummary.TotalOthers / totalDays, 2);
            }

            var daysWithData = dailyData.Where(d => d.TotalQty > 0).ToList();
            if (daysWithData.Any())
            {
                overallSummary.DailyAvgTEBusyPercent = Math.Round(daysWithData.Average(d => d.AvgTEBusyPercent), 2);
                overallSummary.DailyAvgSysBusyPercent = Math.Round(daysWithData.Average(d => d.AvgSysBusyPercent), 2);
                overallSummary.DailyAvgOthersPercent = Math.Round(daysWithData.Average(d => d.AvgOthersPercent), 2);
            }

            return overallSummary;
        }

        private async Task GenerateDailySummaryAsync(DateTime date)
        {
            // Delete existing summaries
            var existingSummaries = await _context.CallSummaries
                .Where(cs => cs.SummaryDate.Date == date.Date)
                .ToListAsync();

            if (existingSummaries.Any())
                _context.CallSummaries.RemoveRange(existingSummaries);

            var newSummaries = new List<CallSummary>();

            for (int hour = 0; hour < 24; hour++)
            {
                var callsInHour = await _context.CallRecords
                    .Where(cr => cr.CallDate.Date == date.Date && cr.CallTime.Hours == hour)
                    .ToListAsync();

                var teBusyCount = callsInHour.Count(cr => cr.CallCloseReason == 0);
                var sysBusyCount = callsInHour.Count(cr => cr.CallCloseReason == 1);
                var othersCount = callsInHour.Count(cr => cr.CallCloseReason >= 2);
                var totalQty = callsInHour.Count;

                var summary = new CallSummary
                {
                    SummaryDate = date.Date, // ✅ SUDAH BENAR - pastikan selalu .Date
                    HourGroup = hour,
                    TotalQty = totalQty,
                    TEBusyCount = teBusyCount,
                    SysBusyCount = sysBusyCount,
                    OthersCount = othersCount,
                    TEBusyPercent = totalQty > 0 ? Math.Round((decimal)teBusyCount / totalQty * 100, 2) : 0,
                    SysBusyPercent = totalQty > 0 ? Math.Round((decimal)sysBusyCount / totalQty * 100, 2) : 0,
                    OthersPercent = totalQty > 0 ? Math.Round((decimal)othersCount / totalQty * 100, 2) : 0,
                    CreatedAt = DateTime.UtcNow
                };

                newSummaries.Add(summary);
            }

            await _context.CallSummaries.AddRangeAsync(newSummaries);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Generated daily summary for {Date}", date.Date);
        }

        public async Task<bool> RegenerateSummariesAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                var currentDate = startDate.Date;
                while (currentDate <= endDate.Date)
                {
                    await GenerateDailySummaryAsync(currentDate);
                    currentDate = currentDate.AddDays(1);
                }

                _logger.LogInformation("Regenerated summaries from {StartDate} to {EndDate}", startDate.Date, endDate.Date);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regenerating summaries");
                return false;
            }
        }

        public async Task<bool> DeleteCallRecordsAsync(DateTime date)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
    
            try
            {
                _logger.LogInformation("🗑️ Starting delete operation for date: {Date}", date.ToString("yyyy-MM-dd"));
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // DELETE dalam transaction tunggal
                var callRecordsDeleted = await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM CallRecords WHERE CallDate = {0}", 
                    date.Date
                );

                var summariesDeleted = await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM CallSummaries WHERE SummaryDate = {0}", 
                    date.Date
                );

                await transaction.CommitAsync();
                stopwatch.Stop();
                
                _logger.LogInformation("🎯 Delete completed in {Ms}ms - CallRecords: {CallRecordCount}, Summaries: {SummaryCount}", 
                    stopwatch.ElapsedMilliseconds, callRecordsDeleted, summariesDeleted);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "❌ Error deleting call records for {Date}", date.Date);
                return false;
            }
        }

        private static CallRecordDto ToDto(CallRecord callRecord) => new()
        {
            CallRecordId = callRecord.CallRecordId,
            CallDate = callRecord.CallDate,
            CallTime = callRecord.CallTime,
            CallCloseReason = callRecord.CallCloseReason,
            CloseReasonDescription = callRecord.GetCloseReasonDescription(),
            HourGroup = callRecord.GetHourGroup(),
            CreatedAt = callRecord.CreatedAt
        };

        public async Task<bool> IsFileAlreadyImported(string fileName)
        {
            try
            {
                // Cek apakah file sudah pernah diimport (berdasarkan nama file)
                return await _context.FileImportHistories
                    .AnyAsync(f => f.FileName == fileName && f.Status == "Completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if file is already imported: {FileName}", fileName);
                return false; // Return false jika ada error, biarkan proses continue
            }
        }

        public async Task ResetAllDataAsync()
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                _logger.LogWarning("🗑️ Starting database reset...");
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                // Truncate tables (paling cepat)
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE CallSummaries");
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE CallRecords");
                await _context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE FileImportHistories");

                await transaction.CommitAsync();
                stopwatch.Stop();
                
                _logger.LogWarning("✅ Database reset completed in {Ms}ms", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "❌ Error resetting database");
                throw;
            }
        }   
        
    }
}