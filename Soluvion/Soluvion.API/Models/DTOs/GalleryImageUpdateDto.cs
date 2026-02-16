namespace Soluvion.API.Models.DTOs
{
    public class GalleryImageUpdateDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty; // A kép címe egyelőre marad string

        // Kategória név többnyelvűsítése
        public Dictionary<string, string> CategoryName { get; set; } = new();

        public int OrderIndex { get; set; }
    }
}