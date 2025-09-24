using System.ComponentModel.DataAnnotations;

namespace Pm.DTOs
{
    public class CreateUserDto
    {
        [Required(ErrorMessage = "Username wajib diisi")]
        [StringLength(50, ErrorMessage = "Username maksimal 50 karakter")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password wajib diisi")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password minimal 8 karakter")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nama lengkap wajib diisi")]
        [StringLength(100, ErrorMessage = "Nama lengkap maksimal 100 karakter")]
        public string FullName { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [StringLength(50, ErrorMessage = "Email maksimal 50 karakter")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Role wajib dipilih")]
        public int? RoleId { get; set; }

    }
}