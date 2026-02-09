using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Soluvion.API.Models
{
    public class Service
    {
        public int Id { get; set; }

        public int CompanyId { get; set; }
        public Company? Company { get; set; }
        
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public int OrderIndex { get; set; }

        [MaxLength(50)]
        public string? Category { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public int DefaultDuration { get; set; }  //"Display" célra, ha nincs variáció.

        [Column(TypeName = "decimal(10, 2)")]
        public decimal DefaultPrice { get; set; }

        [MaxLength(255)]
        public string PictogramLink { get; set; } = string.Empty;

        public List<ServiceVariant> Variants { get; set; } = new List<ServiceVariant>();
    }
}
