// DTOs/Common/PagedResultDto.cs
namespace Pm.DTOs.Common
{
    public class PagedResultDto<T>
    {
        public List<T> Data { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNext => Page < TotalPages;
        public bool HasPrevious => Page > 1;

        // CONSTRUCTOR LAMA (untuk API lain)
        public PagedResultDto(List<T> data, int page, int pageSize, int totalCount)
        {
            Data = data;
            Page = page;
            PageSize = pageSize;
            TotalCount = totalCount;
        }

        // CONSTRUCTOR BARU (untuk InspeksiTemuanKpc)
        public PagedResultDto(List<T> data, BaseQueryDto query, int totalCount)
            : this(data, query.Page, query.PageSize, totalCount)
        {
        }
    }
}