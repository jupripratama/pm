using System.ComponentModel.DataAnnotations;

namespace Pm.DTOs
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Password lama wajib diisi")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password baru wajib diisi")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password minimal 8 karakter")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konfirmasi password wajib diisi")]
        [Compare("NewPassword", ErrorMessage = "Konfirmasi password tidak cocok")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}