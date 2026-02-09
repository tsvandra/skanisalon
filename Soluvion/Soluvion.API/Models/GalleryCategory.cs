using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Soluvion.API.Models
{
    public class GalleryCategory
    {
        public int Id { get; set; }

        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;
        public int CompanyId { get; set; }

        [JsonIgnore]
        public ICollection<GalleryImage>? Images { get; set; }
    }
}
