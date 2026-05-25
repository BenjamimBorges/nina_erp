using System.ComponentModel.DataAnnotations;

namespace ERP.Shared.Dtos
{
    public class CreateClientDto
    {
        [Required(ErrorMessage = "Nome é obrigatório.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Document { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        [Required]
        public int CompanyId { get; set; }
    }
}
