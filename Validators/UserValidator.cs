using FluentValidation;
using Pm.DTOs;
using Pm.Services;

namespace Pm.Validators
{
    public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
    {
        private readonly IUserService _userService;

        public CreateUserDtoValidator(IUserService userService)
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

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Nama lengkap wajib diisi")
                .MaximumLength(100).WithMessage("Nama lengkap maksimal 100 karakter");

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Format email tidak valid")
                .MaximumLength(100).WithMessage("Email maksimal 100 karakter")
                .MustAsync(async (email, cancellation) =>
                    string.IsNullOrEmpty(email) || !await _userService.IsEmailExistsAsync(email))
                .WithMessage("Email sudah digunakan");

            RuleFor(x => x.RoleId)
                .NotNull().WithMessage("Role wajib dipilih")
                .MustAsync(async (roleId, cancellation) =>
                    roleId == null || await _userService.RoleExistsAsync(roleId.Value))
                .WithMessage("Role tidak valid");


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

    public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
    {
        private readonly IUserService _userService;

        public UpdateUserDtoValidator(IUserService userService)
        {
            _userService = userService;

            RuleFor(x => x.Username)
                .MaximumLength(50).WithMessage("Username maksimal 50 karakter");

            RuleFor(x => x.FullName)
                .MaximumLength(100).WithMessage("Nama lengkap maksimal 100 karakter");

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Format email tidak valid")
                .MaximumLength(100).WithMessage("Email maksimal 100 karakter");

            RuleFor(x => x.RoleId)
                .MustAsync(async (roleId, cancellation) =>
                    roleId == null || await _userService.RoleExistsAsync(roleId.Value))
                .WithMessage("Role tidak valid");

        }
    }
}