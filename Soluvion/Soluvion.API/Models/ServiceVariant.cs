using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Soluvion.API.Models
{
    public class ServiceVariant
    {
        public int Id { get; set; }
        public int ServiceId { get; set; }

        [JsonIgnore]
        public Service? Service { get; set; }

        [MaxLength(50)]
        public string VariantName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10, 2)")]
        public decimal Price { get; set; }
        public int Duration { get; set; } // in minutes
    }
}
