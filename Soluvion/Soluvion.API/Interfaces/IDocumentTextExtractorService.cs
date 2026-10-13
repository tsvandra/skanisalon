namespace Soluvion.API.Interfaces
{
    public interface IDocumentTextExtractorService
    {
        /// <summary>
        /// Ellenőrzi, hogy a megadott fájl szöveges/táblázatos dokumentum-e (Excel, Word, PDF).
        /// </summary>
        bool IsExtractableDocument(string fileName, string? contentType);

        /// <summary>
        /// Kinyeri a szöveges és táblázatos tartalmat a dokumentumból (Excel, Word, PDF) markdown/szöveges formátumban.
        /// </summary>
        Task<string?> ExtractTextAsync(IFormFile file);
    }
}

