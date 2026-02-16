using System.ComponentModel.DataAnnotations;

namespace Soluvion.API.Models
{
    public class Company
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public int CompanyTypeId { get; set; }
        public CompanyType? CompanyType { get; set; }
        public bool IsDeleted { get; set; } = false; 
        public DateTime CreationDate { get; set; } = DateTime.UtcNow; 
        
        //Address
        [MaxLength(100)]
        public string State { get; set; } = string.Empty;
        [MaxLength(100)]
        public string City { get; set; } = string.Empty;
        [MaxLength(100)]
        public string StreetName { get; set; } = string.Empty;
        [MaxLength(20)]
        public string HouseNumber { get; set; } = string.Empty;
        [MaxLength(8)]
        public string PostalCode { get; set; } = string.Empty;

        // Kapcsolattartás
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;
        [MaxLength(50)]
        public string Phone { get; set; } = string.Empty;

        // Social media mezok
        [MaxLength(500)]
        public string? FacebookUrl { get; set; }
        [MaxLength(500)]
        public string? InstagramUrl { get; set; }
        [MaxLength(500)]
        public string? TikTokUrl { get; set; }

        public string? MapEmbedUrl { get; set; }


        [MaxLength(100)]
        public string OpeningHoursTitle { get; set; } = "Bejelentkezés alapján";
        // 2. Leírás (Pl: "Jelenleg kizárólag...")
        [MaxLength(500)]
        public string? OpeningHoursDescription { get; set; }
        // 3. Időpontok (HTML-t is engedünk benne a sortörés miatt)
        [MaxLength(500)]
        public string? OpeningTimeSlots { get; set; }
        // 4. Extra infó (Pl: "A hirtelen felszabaduló...")
        [MaxLength(500)]
        public string? OpeningExtraInfo { get; set; }

        // Cégadatok (Szlovák specifikus mezők a modelled alapján)
        [MaxLength(8)]
        public string ICO { get; set; } = string.Empty;
        [MaxLength(10)]
        public string DIC { get; set; } = string.Empty;
        [MaxLength(13)]
        public string ICDPH { get; set; } = string.Empty;

        // Elfogadta-e a szerződést
        public bool AcceptedCompanyUserAgreement { get; set; }


        // Design
        [MaxLength(500)]
        public string? LogoUrl { get; set; } 

        [MaxLength(200)]
        public string? LogoPublicId { get; set; }
        public int LogoHeight { get; set; } = 50;

        [MaxLength(500)]
        public string? HeroImageUrl { get; set; }
        [MaxLength(200)]
        public string? HeroImagePublicId { get; set; }

        [MaxLength(500)]
        public string? FooterImageUrl { get; set; } 

        [MaxLength(200)]
        public string? FooterImagePublicId { get; set; }
        public int FooterHeight { get; set; } = 250;

        [MaxLength (20)]
        public string PrimaryColor {  get; set; } = "#d4af37";
        [MaxLength(20)]
        public string SecondaryColor { get; set; } = "#1a1a1a";
        [MaxLength(5)]
        public string DefaultLanguage { get; set; } = "hu";

        public ICollection<CompanyLanguage> Languages { get; set; } = new List<CompanyLanguage>();
        public ICollection<UiTranslationOverride> TranslationOverrides { get; set; } = new List<UiTranslationOverride>();
    }
}