using FluentValidation;
using Pm.DTOs.Auth;
using Pm.Services;

namespace Pm.Validators
{
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        private readonly IUserService _userService;

        public RegisterDtoValidator(IUserService userService)
        {
            _userService = userService;

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username wajib diisi")
                .MaximumLength(50).WithMessage("Username maksimal 50 karakter")
                .MustAsync(async (username, cancellation) => !await _userService.IsUsernameExistsAsync(username))
                .WithMessage("Username sudah digunakan");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password wajib diisi")
                .MinimumLength(8).WithMessage("Password minimal 8 karakter")
                .Must(BeStrongPassword).WithMessage("Password harus mengandung huruf besar, huruf kecil, angka, dan simbol");

            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Konfirmasi password wajib diisi")
                .Equal(x => x.Password).WithMessage("Konfirmasi password tidak cocok");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Nama lengkap wajib diisi")
                .MaximumLength(100).WithMessage("Nama lengkap maksimal 100 karakter");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email wajib diisi")
                .EmailAddress().WithMessage("Format email tidak valid")
                .MaximumLength(50).WithMessage("Email maksimal 50 karakter")
                .MustAsync(async (email, cancellation) => !await _userService.IsEmailExistsAsync(email))
                .WithMessage("Email sudah digunakan");
        }

        private bool BeStrongPassword(string password)
        {
            return password.Length >= 8
                && password.Any(char.IsUpper)
                && password.Any(char.IsLower)
                && password.Any(char.IsDigit)
                && password.Any(ch => !char.IsLetterOrDigit(ch));
        }
    }
}