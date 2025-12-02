using System.ComponentModel.DataAnnotations;

namespace Pm.DTOs
{
    public class ActivateUserDto
    {
        [Required(ErrorMessage = "IsActive wajib diisi")]
        public bool IsActive { get; set; }
    }
}