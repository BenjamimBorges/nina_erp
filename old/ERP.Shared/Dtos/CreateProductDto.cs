using System.ComponentModel.DataAnnotations;

namespace ERP.Shared.Dtos
{
    public class CreateProductDto
    {
        [Required(ErrorMessage = "Nome é obrigatório.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Sku { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Range(0, double.MaxValue, ErrorMessage = "Preço não pode ser negativo.")]
        public decimal Price { get; set; }

        [Required]
        public int CompanyId { get; set; }
    }
}
