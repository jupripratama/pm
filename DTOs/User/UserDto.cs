using System.ComponentModel.DataAnnotations;

namespace Pm.DTOs
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhotoUrl { get; set; } // Added for profile photo
        public bool IsActive { get; set; }
        public int RoleId { get; set; }
        public string? RoleName { get; set; }
        public string? LastLoginText { get; set; }
        public string? CreatedAtText { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Permissions { get; set; } = new();
    }

    public class UploadPhotoDto
    {
        [Required(ErrorMessage = "File photo wajib diupload")]
        public IFormFile Photo { get; set; } = null!;
    }

    public class UpdatePhotoResponseDto
    {
        public string PhotoUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}