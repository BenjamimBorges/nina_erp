using System.ComponentModel.DataAnnotations;

namespace ERP.Shared.Dtos
{
    public class CreateStockDto
    {
        [Required]
        public int ProductId { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Quantidade não pode ser negativa.")]
        public int Quantity { get; set; }

        [MaxLength(100)]
        public string Location { get; set; } = string.Empty;

        [Required]
        public int CompanyId { get; set; }
    }
}
