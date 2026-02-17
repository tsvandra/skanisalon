namespace Soluvion.API.Models.DTOs
{
    public class CompanySettingsDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        // Cím
        public string City { get; set; }
        public string StreetName { get; set; }
        public string HouseNumber { get; set; }
        public string PostalCode { get; set; }
        public string State { get; set; }

        // Social
        public string? FacebookUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? TikTokUrl { get; set; }
        public string? MapEmbedUrl { get; set; }

        // Nyitvatartás
        public string? OpeningHoursTitle { get; set; }
        public string? OpeningHoursDescription { get; set; }
        public string? OpeningTimeSlots { get; set; }
        public string? OpeningExtraInfo { get; set; }

        // Design
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public int FooterHeight { get; set; }
        public int LogoHeight { get; set; }

        // Képek (Csak olvashatóak itt, feltöltés külön endpointon megy)
        public string? LogoUrl { get; set; }
        public string? HeroImageUrl { get; set; }
        public string? FooterImageUrl { get; set; }
    }
}