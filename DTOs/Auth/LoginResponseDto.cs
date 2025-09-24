namespace Pm.DTOs.Auth
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }
        public UserDto User { get; set; } = null!;
        public List<string> Permissions { get; set; } = new();
    }
}