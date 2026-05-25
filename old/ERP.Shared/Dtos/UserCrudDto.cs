using System.ComponentModel.DataAnnotations;
using ERP.Shared.Enums;

namespace ERP.Shared.Dtos
{
    public class CreateUserDto
    {
        [Required(ErrorMessage = "Username é obrigatório.")]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Senha é obrigatória.")]
        [MinLength(6, ErrorMessage = "Senha deve ter no mínimo 6 caracteres.")]
        public string Password { get; set; } = string.Empty;

        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.User;

        [Required]
        public int CompanyId { get; set; }
    }

    public class UpdateUserDto
    {
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.User;
    }

    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(6, ErrorMessage = "Nova senha deve ter no mínimo 6 caracteres.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
