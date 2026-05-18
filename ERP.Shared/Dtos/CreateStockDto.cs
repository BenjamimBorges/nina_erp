namespace ERP.Shared.Dtos
{
    public class CreateStockDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string Location { get; set; } = string.Empty;
        public int CompanyId { get; set; }
    }
}
