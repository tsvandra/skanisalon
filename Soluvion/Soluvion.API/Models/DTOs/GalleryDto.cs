namespace Soluvion.API.Models.DTOs
{
    public class GalleryImageDto
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } // A frontend ezt várja
        public Dictionary<string, string> Title { get; set; }
        public int CategoryId { get; set; }
        public Dictionary<string, string> Category { get; set; } // A frontend ezt várja
        public int OrderIndex { get; set; }
    }

    public class GalleryCategoryDto
    {
        public int Id { get; set; }
        public Dictionary<string, string> Name { get; set; }
        public int OrderIndex { get; set; }
    }
}