using System.ComponentModel.DataAnnotations;

namespace Pm.DTOs.Auth
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Username wajib diisi")]
        [StringLength(50, ErrorMessage = "Username maksimal 50 karakter")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password wajib diisi")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password minimal 8 karakter")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konfirmasi password wajib diisi")]
        [Compare("Password", ErrorMessage = "Konfirmasi password tidak cocok")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nama lengkap wajib diisi")]
        [StringLength(100, ErrorMessage = "Nama lengkap maksimal 100 karakter")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email wajib diisi")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        [StringLength(50, ErrorMessage = "Email maksimal 50 karakter")]
        public string Email { get; set; } = string.Empty;
    }
}