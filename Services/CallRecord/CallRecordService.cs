using Microsoft.EntityFrameworkCore;
using EFCore.BulkExtensions;
using System.Text.RegularExpressions;
using Pm.Data;
using Pm.DTOs.CallRecord;
using Pm.DTOs.Common;
using Pm.Models;
using System.Globalization;
using System.Text;
using Microsoft.VisualBasic.FileIO;

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
                using var reader = new StreamReader(csvStream, Encoding.UTF8);
                var allContent = await reader.ReadToEndAsync();
                var lines = allContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                _logger.LogInformation("🚀 STARTING CSV IMPORT");
                _logger.LogInformation("📁 File: {FileName}", fileName);
                _logger.LogInformation("📊 Total lines: {Count}", lines.Length);

                // Process in smaller batches for debugging
                var testLines = lines.Take(5).ToArray(); // Hanya proses 5 baris dulu
                _logger.LogInformation("🔍 Testing first {Count} lines only", testLines.Length);

                var records = new List<CallRecord>();
                var successCount = 0;
                var failCount = 0;

                for (int i = 0; i < testLines.Length; i++)
                {
                    _logger.LogInformation("---");
                    var record = ParseCsvRowFlexible(testLines[i], i + 1);
                    if (record != null)
                    {
                        records.Add(record);
                        successCount++;
                        _logger.LogInformation("✅ Row {RowNumber} - PARSED SUCCESSFULLY", i + 1);
                    }
                    else
                    {
                        failCount++;
                        _logger.LogInformation("❌ Row {RowNumber} - FAILED TO PARSE", i + 1);
                    }
                }

                _logger.LogInformation("=== IMPORT SUMMARY ===");
                _logger.LogInformation("✅ Successful: {Success}", successCount);
                _logger.LogInformation("❌ Failed: {Failed}", failCount);
                _logger.LogInformation("📊 Total processed: {Total}", testLines.Length);

                if (records.Any())
                {
                    _logger.LogInformation("💾 Attempting to insert {Count} records to database...", records.Count);
                    await BulkInsertAsync(records);
                    _logger.LogInformation("💾 Database insert completed");
                }
                else
                {
                    _logger.LogWarning("⚠️ No records to insert - all parsing failed!");
                }

                response.SuccessfulRecords = records.Count;
                response.TotalRecords = lines.Length;
                response.FailedRecords = lines.Length - records.Count;

                stopwatch.Stop();
                _logger.LogInformation("⏱️ Import completed in {Ms}ms", stopwatch.ElapsedMilliseconds);

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Import error for {FileName}", fileName);
                response.Errors.Add(ex.Message);
                return response;
            }
        }


        private CallRecord? ParseCsvRowFlexible(string line, int rowNumber)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                _logger.LogWarning("Row {RowNumber}: Empty or whitespace line", rowNumber);
                return null;
            }

            try
            {
                _logger.LogInformation("=== PARSING ROW {RowNumber} ===", rowNumber);
                _logger.LogInformation("Raw line: '{Line}'", line);

                // HAPUS QUOTES DI AWAL DAN AKHIR BARIS
                if (line.StartsWith('"') && line.EndsWith('"'))
                {
                    line = line.Substring(1, line.Length - 2);
                    _logger.LogInformation("Removed surrounding quotes. New line: '{Line}'", line);
                }

                var parts = line.Split(',');
                _logger.LogInformation("Row {RowNumber}: Found {Count} columns", rowNumber, parts.Length);

                if (parts.Length < 3)
                {
                    _logger.LogWarning("Row {RowNumber}: Too few columns ({Count})", rowNumber, parts.Length);
                    return null;
                }

                // Log detail untuk 3 baris pertama
                if (rowNumber <= 3)
                {
                    _logger.LogInformation("Row {RowNumber} DETAILED ANALYSIS:", rowNumber);
                    _logger.LogInformation("  First column [0]: '{First}' (Length: {FirstLength})", parts[0], parts[0].Length);
                    _logger.LogInformation("  Second column [1]: '{Second}' (Length: {SecondLength})", parts[1], parts[1].Length);
                    _logger.LogInformation("  Second last column [{SecondLastIndex}]: '{SecondLast}'", parts.Length - 2, parts[parts.Length - 2]);
                    _logger.LogInformation("  Last column [{LastIndex}]: '{Last}'", parts.Length - 1, parts[parts.Length - 1]);
                }

                // 1. Parse Date (kolom pertama) - SEKARANG SUDAH TANPA QUOTE
                var dateStr = parts[0].Trim();
                _logger.LogInformation("Row {RowNumber}: Date string = '{DateStr}'", rowNumber, dateStr);

                DateTime callDate;

                if (dateStr.Length == 8 && int.TryParse(dateStr, out int dateInt))
                {
                    var year = dateInt / 10000;
                    var month = (dateInt / 100) % 100;
                    var day = dateInt % 100;

                    _logger.LogInformation("Row {RowNumber}: Date components = {Year}-{Month}-{Day}", rowNumber, year, month, day);

                    if (year >= 2000 && year <= 2100 && month >= 1 && month <= 12 && day >= 1 && day <= 31)
                    {
                        callDate = new DateTime(year, month, day);
                        _logger.LogInformation("Row {RowNumber}: ✅ Date parsed successfully: {Date}", rowNumber, callDate.ToString("yyyy-MM-dd"));
                    }
                    else
                    {
                        _logger.LogWarning("Row {RowNumber}: ❌ Invalid date components: {Year}-{Month}-{Day}", rowNumber, year, month, day);
                        return null;
                    }
                }
                else
                {
                    _logger.LogWarning("Row {RowNumber}: ❌ Invalid date format or length: '{DateStr}' (Length: {Length})", rowNumber, dateStr, dateStr.Length);
                    return null;
                }

                // 2. Parse Time (kolom kedua)
                var timeStr = parts[1].Trim();
                _logger.LogInformation("Row {RowNumber}: Time string = '{TimeStr}'", rowNumber, timeStr);

                if (!TimeSpan.TryParse(timeStr, out var callTime))
                {
                    _logger.LogWarning("Row {RowNumber}: ❌ Invalid time format: '{TimeStr}'", rowNumber, timeStr);
                    return null;
                }
                _logger.LogInformation("Row {RowNumber}: ✅ Time parsed successfully: {Time}", rowNumber, callTime);

                // 3. Parse Close Reason (kolom KEDUA TERAKHIR) - SEKARANG SUDAH TANPA QUOTE
                var secondLastIndex = parts.Length - 2;
                var reasonStr = parts[secondLastIndex].Trim();

                _logger.LogInformation("Row {RowNumber}: Close reason string = '{ReasonStr}' (from column {Index})", rowNumber, reasonStr, secondLastIndex);

                if (!int.TryParse(reasonStr, out int callCloseReason))
                {
                    _logger.LogWarning("Row {RowNumber}: ❌ Invalid close reason: '{ReasonStr}'", rowNumber, reasonStr);
                    return null;
                }
                _logger.LogInformation("Row {RowNumber}: ✅ Close reason parsed successfully: {Reason}", rowNumber, callCloseReason);

                _logger.LogInformation("Row {RowNumber}: ✅✅✅ SUCCESS - Date: {Date}, Time: {Time}, Reason: {Reason}",
                    rowNumber, callDate.ToString("yyyy-MM-dd"), callTime, callCloseReason);

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
                _logger.LogError(ex, "Row {RowNumber}: ❌ Unexpected parsing error", rowNumber);
                return null;
            }
        }
        
        private CallRecord? ParseCsvWithTextFieldParser(string line, int rowNumber)
        {
            try
            {
                // Gunakan StringReader untuk mem-parsing CSV dengan benar
                using var reader = new StringReader(line);
                using var parser = new TextFieldParser(reader);
                
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true; // Handle quotes dengan benar
                
                var fields = parser.ReadFields();
                if (fields == null || fields.Length < 3) return null;

                // Sekarang fields sudah bersih dari quotes
                var dateStr = fields[0].Trim();
                var timeStr = fields[1].Trim();
                var reasonStr = fields[fields.Length - 2].Trim();

                // Parse seperti biasa...
                if (dateStr.Length == 8 && int.TryParse(dateStr, out int dateInt))
                {
                    var year = dateInt / 10000;
                    var month = (dateInt / 100) % 100;
                    var day = dateInt % 100;
                    
                    if (year >= 2000 && year <= 2100 && month >= 1 && month <= 12 && day >= 1 && day <= 31)
                    {
                        var callDate = new DateTime(year, month, day);
                        
                        if (TimeSpan.TryParse(timeStr, out var callTime) && 
                            int.TryParse(reasonStr, out int callCloseReason))
                        {
                            return new CallRecord
                            {
                                CallDate = callDate,
                                CallTime = callTime,
                                CallCloseReason = callCloseReason,
                                CreatedAt = DateTime.UtcNow
                            };
                        }
                    }
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }


       // Bulk insert menggunakan EFCore.BulkExtensions
        private async Task BulkInsertAsync(List<CallRecord> records)
        {
            if (!records.Any()) return;
    
            try
            {
                // Single massive INSERT untuk <80k records
                var values = new StringBuilder();
                var parameters = new List<object>();
                
                for (int i = 0; i < records.Count; i++)
                {
                    var r = records[i];
                    if (i > 0) values.Append(",");
                    values.Append($"(@p{i*4},@p{i*4+1},@p{i*4+2},@p{i*4+3})");
                    
                    parameters.Add(r.CallDate);
                    parameters.Add(r.CallTime);
                    parameters.Add(r.CallCloseReason);
                    parameters.Add(r.CreatedAt);
                }
                
                var sql = $"INSERT INTO CallRecords(CallDate,CallTime,CallCloseReason,CreatedAt)VALUES{values}";
                await _context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray());
                
                _logger.LogDebug("Single INSERT query executed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk insert error");
                throw;
            }
        }

        private async Task BulkInsertBatchAsync(List<CallRecord> records)
        {
            var valuesList = new StringBuilder();
            var parameters = new List<object>();
            
            for (int i = 0; i < records.Count; i++)
            {
                var r = records[i];
                if (i > 0) valuesList.Append(",");
                valuesList.Append($"(@p{i*4},@p{i*4+1},@p{i*4+2},@p{i*4+3})");
                
                parameters.Add(r.CallDate);
                parameters.Add(r.CallTime);
                parameters.Add(r.CallCloseReason);
                parameters.Add(r.CreatedAt);
            }
            
            var sql = $"INSERT INTO CallRecords(CallDate,CallTime,CallCloseReason,CreatedAt)VALUES{valuesList}";
            await _context.Database.ExecuteSqlRawAsync(sql, parameters.ToArray());
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

                var callDateTime = callDate.Date.Add(callTime);

                return new CallRecord
                {
                    CallDate = callDateTime,
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

        public async Task<byte[]> ExportCallRecordsToCsvAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Exporting call records from {StartDate} to {EndDate}", startDate, endDate);

                // Get all records in date range
                var records = await _context.CallRecords
                    .Where(cr => cr.CallDate >= startDate.Date && cr.CallDate <= endDate.Date)
                    .OrderBy(cr => cr.CallDate)
                    .ThenBy(cr => cr.CallTime)
                    .ToListAsync();

                // Build CSV content
                var csv = new StringBuilder();
                
                // Header
                csv.AppendLine("DATE;TIME;CALL CLOSE REASON");

                // Data rows
                foreach (var record in records)
                {
                    var date = record.CallDate.ToString("yyyyMMdd");
                    var time = record.CallTime.ToString(@"HH\:mm\:ss");
                    var closeReason = record.CallCloseReason;

                    csv.AppendLine($"{date};{time};{closeReason}");
                }

                _logger.LogInformation("Exported {Count} call records to CSV", records.Count);

                return Encoding.UTF8.GetBytes(csv.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting call records to CSV");
                throw;
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