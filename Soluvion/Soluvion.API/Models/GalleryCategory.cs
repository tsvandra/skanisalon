using System.ComponentModel.DataAnnotations;

namespace Soluvion.API.Models
{
    public class GalleryCategory
    {
        public int Id { get; set; }

        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;
        public int CompanyId { get; set; }
    }
}
