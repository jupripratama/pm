using System.ComponentModel.DataAnnotations;

namespace Pm.DTOs
{
    public class UpdateUserDto
    {
        [StringLength(50, ErrorMessage = "Username maksimal 50 karakter")]
        public string? Username { get; set; }

        [StringLength(100, ErrorMessage = "Nama lengkap maksimal 100 karakter")]
        public string? FullName { get; set; }

        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [StringLength(50, ErrorMessage = "Email maksimal 50 karakter")]
        public string? Email { get; set; }

        public int? RoleId { get; set; }

        public bool? IsActive { get; set; }
    }
}