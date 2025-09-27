using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using Pm.DTOs.CallRecord;

namespace Pm.Services
{
    public class ExcelExportService : IExcelExportService
    {
        private readonly ILogger<ExcelExportService> _logger;

        public ExcelExportService(ILogger<ExcelExportService> logger)
        {
            _logger = logger;
            
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public async Task<byte[]> ExportDailySummaryToExcelAsync(DateTime date, DailySummaryDto summary)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add($"Daily Summary {date:yyyy-MM-dd}");

            // Set up headers
            SetupDailySummaryHeaders(worksheet);

            // Fill data
            await FillDailySummaryData(worksheet, summary);

            // Apply styling
            ApplyDailySummaryFormatting(worksheet, summary.HourlyData.Count);

            return package.GetAsByteArray();
        }

        private void SetupDailySummaryHeaders(ExcelWorksheet worksheet)
        {
            // Header row 1
            worksheet.Cells[1, 1].Value = "Time";
            worksheet.Cells[1, 2].Value = "Qty";
            worksheet.Cells[1, 3].Value = "TE Busy";
            worksheet.Cells[1, 4].Value = "TE Busy %";
            worksheet.Cells[1, 5].Value = "Sys Busy";
            worksheet.Cells[1, 6].Value = "Sys Busy %";
            worksheet.Cells[1, 7].Value = "Others";
            worksheet.Cells[1, 8].Value = "Others %";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillPatternType.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }
        }

        private async Task FillDailySummaryData(ExcelWorksheet worksheet, DailySummaryDto summary)
        {
            int row = 2;

            // Fill hourly data
            foreach (var hourlyData in summary.HourlyData.Where(h => h.Qty > 0)) // Only show hours with data
            {
                worksheet.Cells[row, 1].Value = hourlyData.TimeRange;
                worksheet.Cells[row, 2].Value = hourlyData.Qty;
                worksheet.Cells[row, 3].Value = hourlyData.TEBusy;
                worksheet.Cells[row, 4].Value = $"{hourlyData.TEBusyPercent:F2}%";
                worksheet.Cells[row, 5].Value = hourlyData.SysBusy;
                worksheet.Cells[row, 6].Value = $"{hourlyData.SysBusyPercent:F2}%";
                worksheet.Cells[row, 7].Value = hourlyData.Others;
                worksheet.Cells[row, 8].Value = $"{hourlyData.OthersPercent:F2}%";

                row++;
            }

            // Add total row
            worksheet.Cells[row, 1].Value = "Total";
            worksheet.Cells[row, 2].Value = summary.TotalQty;
            worksheet.Cells[row, 3].Value = summary.TotalTEBusy;
            worksheet.Cells[row, 4].Value = $"{summary.AvgTEBusyPercent:F2}%";
            worksheet.Cells[row, 5].Value = summary.TotalSysBusy;
            worksheet.Cells[row, 6].Value = $"{summary.AvgSysBusyPercent:F2}%";
            worksheet.Cells[row, 7].Value = summary.TotalOthers;
            worksheet.Cells[row, 8].Value = $"{summary.AvgOthersPercent:F2}%";

            // Style total row
            using (var range = worksheet.Cells[row, 1, row, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillPatternType.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            row++;

            // Add daily average row
            worksheet.Cells[row, 1].Value = "Daily Average";
            worksheet.Cells[row, 4].Value = $"{summary.AvgTEBusyPercent:F2}%";
            worksheet.Cells[row, 6].Value = $"{summary.AvgSysBusyPercent:F2}%";
            worksheet.Cells[row, 8].Value = $"{summary.AvgOthersPercent:F2}%";

            // Style daily average row
            using (var range = worksheet.Cells[row, 1, row, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillPatternType.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }
        }

        private void ApplyDailySummaryFormatting(ExcelWorksheet worksheet, int dataRowCount)
        {
            // Auto fit columns
            worksheet.Cells.AutoFitColumns();

            // Add borders to all cells
            int lastRow = dataRowCount + 3; // +3 for header, total, and average rows
            using (var range = worksheet.Cells[1, 1, lastRow, 8])
            {
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            // Center align percentage columns
            worksheet.Cells[1, 4, lastRow, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[1, 6, lastRow, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[1, 8, lastRow, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Right align number columns
            worksheet.Cells[1, 2, lastRow, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            worksheet.Cells[1, 5, lastRow, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            worksheet.Cells[1, 7, lastRow, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
        }

        public async Task<byte[]> ExportOverallSummaryToExcelAsync(DateTime startDate, DateTime endDate, OverallSummaryDto summary)
        {
            using var package = new ExcelPackage();
            
            // Create main summary sheet
            var summarySheet = package.Workbook.Worksheets.Add("Overall Summary");
            await FillOverallSummarySheet(summarySheet, startDate, endDate, summary);

            // Create daily breakdown sheet
            var dailySheet = package.Workbook.Worksheets.Add("Daily Breakdown");
            FillDailyBreakdownSheet(dailySheet, summary);

            return package.GetAsByteArray();
        }

        private async Task FillOverallSummarySheet(ExcelWorksheet worksheet, DateTime startDate, DateTime endDate, OverallSummaryDto summary)
        {
            int row = 1;

            // Title
            worksheet.Cells[row, 1].Value = $"Call Record Summary ({startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd})";
            worksheet.Cells[row, 1, row, 8].Merge = true;
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 1].Style.Font.Size = 16;
            worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            row += 2;

            // Period information
            worksheet.Cells[row, 1].Value = "Period:";
            worksheet.Cells[row, 2].Value = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}";
            row++;
            worksheet.Cells[row, 1].Value = "Total Days:";
            worksheet.Cells[row, 2].Value = summary.TotalDays;
            row += 2;

            // Overall totals
            worksheet.Cells[row, 1].Value = "OVERALL TOTALS";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            row++;

            worksheet.Cells[row, 1].Value = "Total Calls:";
            worksheet.Cells[row, 2].Value = summary.TotalCalls;
            worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";
            row++;

            worksheet.Cells[row, 1].Value = "Total TE Busy:";
            worksheet.Cells[row, 2].Value = summary.TotalTEBusy;
            worksheet.Cells[row, 3].Value = $"{summary.TotalAvgTEBusyPercent:F2}%";
            worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";
            row++;

            worksheet.Cells[row, 1].Value = "Total Sys Busy:";
            worksheet.Cells[row, 2].Value = summary.TotalSysBusy;
            worksheet.Cells[row, 3].Value = $"{summary.TotalAvgSysBusyPercent:F2}%";
            worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";
            row++;

            worksheet.Cells[row, 1].Value = "Total Others:";
            worksheet.Cells[row, 2].Value = summary.TotalOthers;
            worksheet.Cells[row, 3].Value = $"{summary.TotalAvgOthersPercent:F2}%";
            worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0";
            row += 2;

            // Average per day
            worksheet.Cells[row, 1].Value = "AVERAGE PER DAY";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            row++;

            worksheet.Cells[row, 1].Value = "Avg Calls/Day:";
            worksheet.Cells[row, 2].Value = (double)summary.AvgCallsPerDay;
            worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";
            row++;

            worksheet.Cells[row, 1].Value = "Avg TE Busy/Day:";
            worksheet.Cells[row, 2].Value = (double)summary.AvgTEBusyPerDay;
            worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";
            row++;

            worksheet.Cells[row, 1].Value = "Avg Sys Busy/Day:";
            worksheet.Cells[row, 2].Value = (double)summary.AvgSysBusyPerDay;
            worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";
            row++;

            worksheet.Cells[row, 1].Value = "Avg Others/Day:";
            worksheet.Cells[row, 2].Value = (double)summary.AvgOthersPerDay;
            worksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0.00";
            row += 2;

            // Daily average percentages
            worksheet.Cells[row, 1].Value = "DAILY AVERAGE PERCENTAGES";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            row++;

            worksheet.Cells[row, 1].Value = "Daily Avg TE Busy %:";
            worksheet.Cells[row, 2].Value = $"{summary.DailyAvgTEBusyPercent:F2}%";
            row++;

            worksheet.Cells[row, 1].Value = "Daily Avg Sys Busy %:";
            worksheet.Cells[row, 2].Value = $"{summary.DailyAvgSysBusyPercent:F2}%";
            row++;

            worksheet.Cells[row, 1].Value = "Daily Avg Others %:";
            worksheet.Cells[row, 2].Value = $"{summary.DailyAvgOthersPercent:F2}%";

            worksheet.Cells.AutoFitColumns();
        }

        private void FillDailyBreakdownSheet(ExcelWorksheet worksheet, OverallSummaryDto summary)
        {
            // Headers
            worksheet.Cells[1, 1].Value = "Date";
            worksheet.Cells[1, 2].Value = "Total Qty";
            worksheet.Cells[1, 3].Value = "TE Busy";
            worksheet.Cells[1, 4].Value = "TE Busy %";
            worksheet.Cells[1, 5].Value = "Sys Busy";
            worksheet.Cells[1, 6].Value = "Sys Busy %";
            worksheet.Cells[1, 7].Value = "Others";
            worksheet.Calls[1, 8].Value = "Others %";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 8])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = ExcelFillPatternType.Solid;
                range.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // Fill daily data
            int row = 2;
            foreach (var dailyData in summary.DailyData)
            {
                worksheet.Cells[row, 1].Value = dailyData.Date.ToString("yyyy-MM-dd");
                worksheet.Cells[row, 2].Value = dailyData.TotalQty;
                worksheet.Cells[row, 3].Value = dailyData.TotalTEBusy;
                worksheet.Cells[row, 4].Value = $"{dailyData.AvgTEBusyPercent:F2}%";
                worksheet.Cells[row, 5].Value = dailyData.TotalSysBusy;
                worksheet.Cells[row, 6].Value = $"{dailyData.AvgSysBusyPercent:F2}%";
                worksheet.Cells[row, 7].Value = dailyData.TotalOthers;
                worksheet.Cells[row, 8].Value = $"{dailyData.AvgOthersPercent:F2}%";

                row++;
            }

            worksheet.Cells.AutoFitColumns();

            // Add borders
            using (var range = worksheet.Cells[1, 1, row - 1, 8])
            {
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }
        }
    }
}