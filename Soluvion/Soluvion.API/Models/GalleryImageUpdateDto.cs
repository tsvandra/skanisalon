namespace Soluvion.API.Models
{
    public class GalleryImageUpdateDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
    }

    public class CategoryDto
    {
        public string Name { get; set; } = string.Empty;
    }
}