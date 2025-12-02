// DTOs/InspeksiTemuanKpcQueryDto.cs
using Pm.DTOs.Common;

namespace Pm.DTOs
{
    public class InspeksiTemuanKpcQueryDto : BaseQueryDto  // GANTI DARI PagedQueryDto KE BaseQueryDto
    {
        public string? Ruang { get; set; }
        public string? Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IncludeDeleted { get; set; } = false;

        public bool? IsArchived { get; set; }
    }
}