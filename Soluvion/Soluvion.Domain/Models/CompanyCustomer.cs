using System.ComponentModel.DataAnnotations.Schema;

namespace Soluvion.Domain.Models
{
    public class CompanyCustomer
    {
        public int Id { get; set; }

        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; }

        // Dinamikus vendég adatok
        [Column(TypeName = "jsonb")]
        public Dictionary<string, string> Attributes { get; set; } = new();

        public string? Notes { get; set; }
    }
}