namespace Pm.DTOs.Common
{
    public class PagedResultDto<T>
    {
        public List<T> Data { get; set; } = new();
        public PaginationMeta Meta { get; set; }

        public PagedResultDto(List<T> data, int page, int pageSize, int totalCount)
        {
            Data = data;
            Meta = new PaginationMeta
            {
                Pagination = new PaginationInfo
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    HasNext = page < (int)Math.Ceiling((double)totalCount / pageSize),
                    HasPrevious = page > 1
                }
            };
        }

        public PagedResultDto(List<T> data, BaseQueryDto query, int totalCount)
            : this(data, query.Page, query.PageSize, totalCount)
        {
        }
    }

    public class PaginationMeta
    {
        public PaginationInfo Pagination { get; set; } = new();
    }

    public class PaginationInfo
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasNext { get; set; }
        public bool HasPrevious { get; set; }
    }
}