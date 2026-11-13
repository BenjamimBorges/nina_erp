namespace ERP.Core.Entities
{
    public class Company : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        // Stored in canonical digits-only form; presentation formatting belongs to the API.
        public string TaxId { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
