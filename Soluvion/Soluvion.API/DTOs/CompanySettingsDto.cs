namespace Soluvion.API.DTOs
{
    public class CompanySettingsDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        // SaaS Funkciók és Beállítások
        public List<string> EnabledFeatures { get; set; } = new();
        public bool IsOnlineBookingEnabled { get; set; }

        // Cím
        public string City { get; set; } = string.Empty;
        public string StreetName { get; set; } = string.Empty;
        public string HouseNumber { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;

        // Social
        public string? FacebookUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? TikTokUrl { get; set; }
        public string? MapEmbedUrl { get; set; }

        // Nyitvatartás
        public Dictionary<string, string> OpeningHoursTitle { get; set; } = new();
        public Dictionary<string, string> OpeningHoursDescription { get; set; } = new();
        public Dictionary<string, string> OpeningTimeSlots { get; set; } = new();
        public Dictionary<string, string> OpeningExtraInfo { get; set; } = new();

        // Design
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public int FooterHeight { get; set; }
        public int LogoHeight { get; set; }

        // Képek
        public string? LogoUrl { get; set; }
        public string? HeroImageUrl { get; set; }
        public string? FooterImageUrl { get; set; }
    }
}
