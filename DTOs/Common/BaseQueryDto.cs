namespace Pm.DTOs.Common
{
    public class BaseQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? SortBy { get; set; }
        public string? SortDir { get; set; } = "desc";
    }
}