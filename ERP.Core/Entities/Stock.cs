namespace ERP.Core.Entities
{
    public class Stock : BaseEntity
    {
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public int Quantity { get; set; }
        public string Location { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public Company? Company { get; set; }
    }
}
