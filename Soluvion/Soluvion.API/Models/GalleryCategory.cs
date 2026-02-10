using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Soluvion.API.Models
{
    public class GalleryCategory
    {
        public int Id { get; set; }

        [MaxLength(50)]
        // Tárolás: JSONB, pl.: {"hu": "Hajvágás", "en": "Haircut"}
        public Dictionary<string, string> Name { get; set; } = new();
        public int OrderIndex { get; set; } = 0;
        public int CompanyId { get; set; }

        [JsonIgnore]
        public ICollection<GalleryImage>? Images { get; set; }
    }
}
