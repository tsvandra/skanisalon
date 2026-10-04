namespace Soluvion.API.DTOs
{
    public class GalleryImageDto
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; } = string.Empty; // A frontend ezt várja
        public Dictionary<string, string> Title { get; set; } = new();
        public int CategoryId { get; set; }
        public Dictionary<string, string> Category { get; set; } = new(); // A frontend ezt várja
        public int OrderIndex { get; set; }
    }

    public class GalleryCategoryDto
    {
        public int Id { get; set; }
        public Dictionary<string, string> Name { get; set; } = new();
        public int OrderIndex { get; set; }
    }

}
