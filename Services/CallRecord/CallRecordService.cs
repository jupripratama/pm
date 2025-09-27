using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Pm.Data;
using Pm.DTOs.CallRecord;
using Pm.DTOs.Common;
using Pm.Models;
using System.Globalization;

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
            var errors = new List<string>();
            var callRecords = new List<CallRecord>();

        try
        {
            using var reader = new StreamReader(csvStream, encoding: System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            
            _logger.LogInformation("Processing large CSV file {FileName}", fileName);
            
            var lineNumber = 0;
            var batchSize = 1000; // Process in batches
            var currentBatch = new List<CallRecord>();

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                lineNumber++;

                // Split merged rows if needed
                var rows = SplitMergedLine(line);
                
                foreach (var row in rows)
                {
                    try
                    {
                        var callRecord = ParseCsvRow(row, lineNumber);
                        if (callRecord != null)
                        {
                            currentBatch.Add(callRecord);
                            response.SuccessfulRecords++;

                            // Save in batches to avoid memory issues
                            if (currentBatch.Count >= batchSize)
                            {
                                await SaveBatchAsync(currentBatch);
                                currentBatch.Clear();
                            }
                        }
                        else
                        {
                            response.FailedRecords++;
                            if (errors.Count < 100) // Limit error collection
                            {
                                errors.Add($"Row {lineNumber}: Invalid data format");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        response.FailedRecords++;
                        if (errors.Count < 100)
                        {
                            errors.Add($"Row {lineNumber}: {ex.Message}");
                        }

                        // Log first few errors in detail
                        if (errors.Count <= 5)
                        {
                            _logger.LogWarning("Row {LineNumber} error: {Error}. Data: {Data}", 
                                lineNumber, ex.Message, row.Length > 100 ? row[..100] + "..." : row);
                        }
                    }
                    }

                    response.TotalRecords = lineNumber;

                    // Log progress every 10,000 rows
                    if (lineNumber % 10000 == 0)
                    {
                        _logger.LogInformation("Processed {Processed} rows, Success: {Success}, Failed: {Failed}", 
                            lineNumber, response.SuccessfulRecords, response.FailedRecords);
                    }
            }

                // Save remaining batch
                if (currentBatch.Any())
                {
                    await SaveBatchAsync(currentBatch);
                }

                    // Generate summaries for affected dates
                    if (response.SuccessfulRecords > 0)
                    {
                        var affectedDates = await _context.CallRecords
                            .Where(cr => cr.CreatedAt >= DateTime.UtcNow.AddMinutes(-10)) // Records from this session
                            .Select(cr => cr.CallDate.Date)
                            .Distinct()
                            .ToListAsync();

                        _logger.LogInformation("Regenerating summaries for {DateCount} dates", affectedDates.Count);

                        foreach (var date in affectedDates)
                        {
                            await GenerateDailySummaryAsync(date);
                        }
                    }

                    response.Errors = errors;
                    _logger.LogInformation("CSV import completed. Total: {Total}, Success: {Success}, Failed: {Failed}", 
                        response.TotalRecords, response.SuccessfulRecords, response.FailedRecords);

                    return response;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing large CSV file {FileName}", fileName);
                    response.Errors.Add($"General error: {ex.Message}");
                    return response;
                }
        }
        
        private async Task SaveBatchAsync(List<CallRecord> batch)
        {
            if (!batch.Any()) return;

            await _context.CallRecords.AddRangeAsync(batch);
            await _context.SaveChangesAsync();
            
            _logger.LogDebug("Saved batch of {Count} records", batch.Count);
        }

        private List<string> SplitMergedLine(string line)
        {
            var rows = new List<string>();
            
            // Check for multiple date patterns in one line
            var dateMatches = Regex.Matches(line, @"\b20\d{6}\b");
            
            if (dateMatches.Count <= 1)
            {
                rows.Add(line);
            }
            else
            {
                // Split by date pattern
                var parts = Regex.Split(line, @"(?=\b20\d{6}\b)");
                foreach (var part in parts)
                {
                    if (!string.IsNullOrWhiteSpace(part.Trim()))
                    {
                        rows.Add(part.Trim());
                    }
                }
            }
            
            return rows;
        }

        // private List<string> SplitCsvContent(string csvContent)
        // {
        //     var rows = new List<string>();
        //     var lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        //     foreach (var line in lines)
        //     {
        //         var dateMatches = Regex.Matches(line, @"\b20\d{6}\b");

        //         if (dateMatches.Count > 1)
        //         {
        //             var parts = Regex.Split(line, @"(?=\b20\d{6}\b)");
        //             foreach (var part in parts)
        //             {
        //                 if (!string.IsNullOrWhiteSpace(part.Trim()))
        //                 {
        //                     rows.Add(part.Trim());
        //                 }
        //             }
        //         }
        //         else
        //         {
        //             if (!string.IsNullOrWhiteSpace(line.Trim()))
        //             {
        //                 rows.Add(line.Trim());
        //             }
        //         }
        //     }

        //     return rows;
        // }

        private CallRecord? ParseCsvRow(string csvRow, int rowNumber)
        {
            try
            {
                // LOG: Tampilkan 5 baris pertama untuk debug
                if (rowNumber <= 5)
                {
                    _logger.LogInformation("Row {RowNumber} raw data: '{CsvRow}'", rowNumber, csvRow);
                }

                // Clean row dari characters aneh
                csvRow = csvRow.Trim().Trim('\r', '\n');
                
                var fields = csvRow.Split(',');
                
                // LOG: Tampilkan jumlah field dan 3 field pertama
                if (rowNumber <= 5)
                {
                    _logger.LogInformation("Row {RowNumber} has {FieldCount} fields. First 3: ['{Field0}'], ['{Field1}'], ['{Field2}']", 
                        rowNumber, fields.Length, 
                        fields.Length > 0 ? CleanCsvField(fields[0]) : "NULL",
                        fields.Length > 1 ? CleanCsvField(fields[1]) : "NULL", 
                        fields.Length > 2 ? CleanCsvField(fields[2]) : "NULL");
                }
                
                if (fields.Length < 3)
                {
                    throw new ArgumentException($"Insufficient fields: only {fields.Length} fields found");
                }

                // Clean date field
                var dateField = CleanCsvField(fields[0]);
                if (string.IsNullOrEmpty(dateField))
                {
                    throw new ArgumentException("Date field is empty");
                }
                
                if (!DateTime.TryParseExact(dateField, "yyyyMMdd", null, DateTimeStyles.None, out var callDate))
                {
                    throw new ArgumentException($"Invalid date format: '{dateField}' (length: {dateField.Length})");
                }

                // Clean time field  
                var timeField = CleanCsvField(fields[1]);
                if (string.IsNullOrEmpty(timeField))
                {
                    throw new ArgumentException("Time field is empty");
                }
                
                if (!TimeSpan.TryParse(timeField, out var callTime))
                {
                    throw new ArgumentException($"Invalid time format: '{timeField}'");
                }

                // Get close reason dengan lebih detail logging
                int callCloseReason = -1;
                string reasonField = "";
                
                if (fields.Length >= 2)
                {
                    reasonField = CleanCsvField(fields[fields.Length - 2]);
                    if (int.TryParse(reasonField, out int secondLastValue))
                    {
                        callCloseReason = secondLastValue;
                    }
                }
                
                if (callCloseReason == -1 && fields.Length >= 1)
                {
                    reasonField = CleanCsvField(fields[fields.Length - 1]);
                    if (int.TryParse(reasonField, out int lastValue))
                    {
                        callCloseReason = lastValue;
                    }
                }

                if (callCloseReason == -1)
                {
                    throw new ArgumentException($"Invalid call close reason: '{reasonField}' from field count {fields.Length}");
                }

                return new CallRecord
                {
                    CallDate = callDate,
                    CallTime = callTime,
                    CallCloseReason = callCloseReason,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Error parsing row {RowNumber}: {Error}", rowNumber, ex.Message);
                return null;
            }
        }

        private string CleanCsvField(string field)
            {
                if (string.IsNullOrWhiteSpace(field))
                    return "";
                    
                return field.Trim()
                            .Trim('"')
                            .Trim('\'')
                            .Trim()
                            .Replace("\r", "")
                            .Replace("\n", "");
            }

        public async Task<PagedResultDto<CallRecordDto>> GetCallRecordsAsync(CallRecordQueryDto query)
        {
            var dbQuery = _context.CallRecords.AsQueryable();

            // Apply filters
            if (query.StartDate.HasValue)
            {
                dbQuery = dbQuery.Where(cr => cr.CallDate >= query.StartDate.Value.Date);
            }

            if (query.EndDate.HasValue)
            {
                dbQuery = dbQuery.Where(cr => cr.CallDate <= query.EndDate.Value.Date);
            }

            if (query.CallCloseReason.HasValue)
            {
                dbQuery = dbQuery.Where(cr => cr.CallCloseReason == query.CallCloseReason.Value);
            }

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
            if (!string.IsNullOrWhiteSpace(query.SortBy))
            {
                dbQuery = query.SortBy.ToLower() switch
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
                    _ => dbQuery.OrderByDescending(cr => cr.CreatedAt)
                };
            }
            else
            {
                dbQuery = dbQuery.OrderByDescending(cr => cr.CallDate).ThenByDescending(cr => cr.CallTime);
            }

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

            // Calculate daily percentages
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
                OthersPercent = s.OthersPercent
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

            // Calculate total average percentages (dari keseluruhan data)
            if (overallSummary.TotalCalls > 0)
            {
                overallSummary.TotalAvgTEBusyPercent = Math.Round((decimal)overallSummary.TotalTEBusy / overallSummary.TotalCalls * 100, 2);
                overallSummary.TotalAvgSysBusyPercent = Math.Round((decimal)overallSummary.TotalSysBusy / overallSummary.TotalCalls * 100, 2);
                overallSummary.TotalAvgOthersPercent = Math.Round((decimal)overallSummary.TotalOthers / overallSummary.TotalCalls * 100, 2);
            }

            // Calculate averages per day (rata-rata jumlah per hari)
            if (totalDays > 0)
            {
                overallSummary.AvgCallsPerDay = Math.Round((decimal)overallSummary.TotalCalls / totalDays, 2);
                overallSummary.AvgTEBusyPerDay = Math.Round((decimal)overallSummary.TotalTEBusy / totalDays, 2);
                overallSummary.AvgSysBusyPerDay = Math.Round((decimal)overallSummary.TotalSysBusy / totalDays, 2);
                overallSummary.AvgOthersPerDay = Math.Round((decimal)overallSummary.TotalOthers / totalDays, 2);
            }

            // Calculate daily average percentages (rata-rata dari persentase harian)
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
            // Delete existing summaries for the date
            var existingSummaries = await _context.CallSummaries
                .Where(cs => cs.SummaryDate.Date == date.Date)
                .ToListAsync();

            if (existingSummaries.Any())
            {
                _context.CallSummaries.RemoveRange(existingSummaries);
            }

            // Generate new summaries for each hour (0-23)
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
                    SummaryDate = date.Date,
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
                _logger.LogError(ex, "Error regenerating summaries from {StartDate} to {EndDate}", startDate.Date, endDate.Date);
                return false;
            }
        }

        public async Task<bool> DeleteCallRecordsAsync(DateTime date)
        {
            try
            {
                var callRecords = await _context.CallRecords
                    .Where(cr => cr.CallDate.Date == date.Date)
                    .ToListAsync();

                var summaries = await _context.CallSummaries
                    .Where(cs => cs.SummaryDate.Date == date.Date)
                    .ToListAsync();

                if (callRecords.Any())
                {
                    _context.CallRecords.RemoveRange(callRecords);
                }

                if (summaries.Any())
                {
                    _context.CallSummaries.RemoveRange(summaries);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted {CallRecordCount} call records and {SummaryCount} summaries for {Date}", 
                    callRecords.Count, summaries.Count, date.Date);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting call records for {Date}", date.Date);
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
    }
}