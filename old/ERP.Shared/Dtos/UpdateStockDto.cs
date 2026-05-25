namespace ERP.Shared.Dtos
{
    public class UpdateStockDto
    {
        public int Quantity { get; set; }
        public string Location { get; set; } = string.Empty;
    }
}
