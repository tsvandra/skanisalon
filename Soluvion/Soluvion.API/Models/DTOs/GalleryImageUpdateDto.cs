namespace Soluvion.API.Models.DTOs
{
    public class GalleryImageUpdateDto
    {
        public int Id { get; set; }
        public Dictionary<string, string>? Title { get; set; } // A kép címe egyelőre marad string

        // Kategória név többnyelvűsítése
        public Dictionary<string, string> CategoryName { get; set; } = new();

        public int OrderIndex { get; set; }
    }
}