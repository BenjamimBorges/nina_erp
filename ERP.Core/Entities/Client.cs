namespace ERP.Core.Entities
{
    public class Client : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Document { get; set; } = string.Empty; // CPF/CNPJ
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public Company? Company { get; set; }
    }
}
