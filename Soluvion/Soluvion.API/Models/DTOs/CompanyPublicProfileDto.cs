using Soluvion.API.Models;

namespace Soluvion.API.DTOs
{
    public class CompanyPublicProfileDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // Design
        public string? LogoUrl { get; set; }
        public int LogoHeight { get; set; }
        public string? FooterImageUrl { get; set; }
        public int FooterHeight { get; set; }
        public string? HeroImageUrl { get; set; }
        public string PrimaryColor { get; set; } = string.Empty;
        public string SecondaryColor { get; set; } = string.Empty;

        // Tartalom / Kapcsolat
        public string? OpeningHoursTitle { get; set; }
        public string? OpeningHoursDescription { get; set; }
        public string? OpeningTimeSlots { get; set; }
        public string? OpeningExtraInfo { get; set; }

        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? FacebookUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? TikTokUrl { get; set; }
        public string? MapEmbedUrl { get; set; }

        public string State { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string StreetName { get; set; } = string.Empty;
        public string HouseNumber { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;

        // Nyelvek
        public string DefaultLanguage { get; set; } = "hu";
        public List<string> SupportedLanguages { get; set; } = new List<string>();

        // A Mester Sablon (hu.json) tartalma
        // Ezt majd a Controller olvassa be a fájlból vagy DB-ből
        // Most egyszerűsítünk: A frontendnek alapból kell a hu.json, 
        // de azt külön hívással is kérheti. Egyelőre itt csak a konfigurációt adjuk vissza.
    }
}