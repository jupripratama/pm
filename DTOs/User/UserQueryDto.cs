using Pm.DTOs.Common;

namespace Pm.DTOs
{
    public class UserQueryDto : BaseQueryDto
    {
        public int? RoleId { get; set; }
        public bool? IsActive { get; set; }
    }
}