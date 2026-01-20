using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Soluvion.API.Models
{
    public class GalleryImage
    {
        public int Id { get; set; }
        public GalleryCategory? Category { get; set; }

        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string ImagePath { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    }
}
