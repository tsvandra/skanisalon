namespace Soluvion.API.Models
{
    public class GalleryImageUpdateDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty; // A kép címe egyelőre marad string

        // Kategória név többnyelvűsítése
        public Dictionary<string, string> CategoryName { get; set; } = new();

        public int OrderIndex { get; set; }
    }

    public class CategoryDto
    {
        // Kategória létrehozás/módosítás többnyelvű névvel
        public Dictionary<string, string> Name { get; set; } = new();

        public int OrderIndex { set; get; }
    }
}