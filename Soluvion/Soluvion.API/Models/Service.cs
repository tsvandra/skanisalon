using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Soluvion.API.Models
{
    public class Service
    {
        public int Id { get; set; }

        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        // Többnyelvűsítés: string -> Dictionary<string, string>
        // Tárolás: JSONB, pl.: {"hu": "Hajvágás", "en": "Haircut"}
        public Dictionary<string, string> Name { get; set; } = new();

        public int OrderIndex { get; set; }

        // Kategória is többnyelvű
        public Dictionary<string, string> Category { get; set; } = new();

        // Leírás is többnyelvű
        public Dictionary<string, string> Description { get; set; } = new();

        public int DefaultDuration { get; set; }  //"Display" célra, ha nincs variáció.

        [Column(TypeName = "decimal(10, 2)")]
        public decimal DefaultPrice { get; set; }

        [MaxLength(255)]
        public string PictogramLink { get; set; } = string.Empty;

        public List<ServiceVariant> Variants { get; set; } = new List<ServiceVariant>();
    }
}