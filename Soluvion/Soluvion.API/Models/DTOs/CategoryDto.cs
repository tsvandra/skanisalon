namespace Soluvion.API.Models.DTOs
{
    public class CategoryDto
    {
        // Kategória létrehozás/módosítás többnyelvű névvel
        public Dictionary<string, string> Name { get; set; } = new();

        public int OrderIndex { set; get; }
    }
}
