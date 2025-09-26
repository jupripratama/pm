using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Pm.Data;
using Pm.DTOs.CallRecord;
using Pm.DTOs.Common;
using Pm.Models;

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
                using var reader = new StreamReader(csvStream);
                var csvContent = await reader.ReadToEndAsync();
                
                var allRows = SplitCsvContent(csvContent);
                response.TotalRecords = allRows.Count;

                _logger.LogInformation("Processing {Count} records from {FileName}", allRows.Count, fileName);

                foreach (var (row, index) in allRows.Select((r, i) => (r, i)))
                {
                    try
                    {
                        var callRecord = ParseCsvRow(row, index + 1);
                        if (callRecord != null)
                        {
                            callRecords.Add(callRecord);
                            response.SuccessfulRecords++;
                        }
                        else
                        {
                            response.FailedRecords++;
                            errors.Add($"Row {index + 1}: Invalid data format");
                        }
                    }
                    catch (Exception ex)
                    {
                        response.FailedRecords++;
                        errors.Add($"Row {index + 1}: {ex.Message}");
                        _logger.LogWarning(ex, "Error processing row {RowIndex}", index + 1);
                    }
                }

                if (callRecords.Any())
                {
                    await _context.CallRecords.AddRangeAsync(callRecords);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Saved {Count} call records to database", callRecords.Count);

                    // Generate summaries for affected dates
                    var affectedDates = callRecords.Select(cr => cr.CallDate.Date).Distinct().ToList();
                    foreach (var date in affectedDates)
                    {
                        await GenerateDailySummaryAsync(date);
                    }
                }

                response.Errors = errors;
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing CSV file {FileName}", fileName);
                response.Errors.Add($"General error: {ex.Message}");
                return response;
            }
        }

        private List<string> SplitCsvContent(string csvContent)
        {
            var rows = new List<string>();
            var lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var dateMatches = Regex.Matches(line, @"\b20\d{6}\b");
                
                if (dateMatches.Count > 1)
                {
                    var parts = Regex.Split(line, @"(?=\b20\d{6}\b)");
                    foreach (var part in parts)
                    {
                        if (!string.IsNullOrWhiteSpace(part.Trim()))
                        {
                            rows.Add(part.Trim());
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(line.Trim()))
                    {
                        rows.Add(line.Trim());
                    }
                }
            }
            
            return rows;
        }

        private CallRecord? ParseCsvRow(string csvRow, int rowNumber)
        {
            try
            {
                var fields = csvRow.Split(',');
                
                if (fields.Length < 3)
                {
                    throw new ArgumentException("Insufficient fields in CSV row");
                }

                // Extract date (first field)
                var dateField = fields[0].Trim();
                if (!DateTime.TryParseExact(dateField, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var callDate))
                {
                    throw new ArgumentException($"Invalid date format: {dateField}");
                }

                // Extract time (second field)
                var timeField = fields[1].Trim();
                if (!TimeSpan.TryParse(timeField, out var callTime))
                {
                    throw new ArgumentException($"Invalid time format: {timeField}");
                }

                // Extract call close reason from second-to-last field
                int callCloseReason = -1;
                
                if (fields.Length >= 2)
                {
                    var secondLastField = fields[fields.Length - 2].Trim();
                    if (int.TryParse(secondLastField, out int secondLastValue))
                    {
                        callCloseReason = secondLastValue;
                    }
                }
                
                if (callCloseReason == -1 && fields.Length >= 1)
                {
                    var lastField = fields[fields.Length - 1].Trim();
                    if (int.TryParse(lastField, out int lastValue))
                    {
                        callCloseReason = lastValue;
                    }
                }

                if (callCloseReason == -1)
                {
                    throw new ArgumentException("Invalid call close reason in row");
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
                dbQuery = dbQuery.Where(cr => cr.HourGroup == query.HourGroup.Value);
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
                    .Where(cr => cr.CallDate.Date == date.Date && cr.HourGroup == hour)
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
            CloseReasonDescription = callRecord.CloseReasonDescription,
            HourGroup = callRecord.HourGroup,
            CreatedAt = callRecord.CreatedAt
        };
    }
}