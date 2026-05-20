using System.ComponentModel.DataAnnotations;

namespace ERP.Shared.Dtos
{
    public class CompanyDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TaxId { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class CreateCompanyDto
    {
        [Required(ErrorMessage = "Nome é obrigatório.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string TaxId { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;
    }

    public class UpdateCompanyDto
    {
        [Required(ErrorMessage = "Nome é obrigatório.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;
    }
}
