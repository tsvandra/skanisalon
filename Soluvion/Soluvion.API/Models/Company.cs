using System.ComponentModel.DataAnnotations;

namespace Soluvion.API.Models
{
    public class Company
    {
        // Ez lesz az elsődleges kulcs (Primary Key)
        public int Id { get; set; }

        // Milyen típusú cég (Fodrászat, Kozmetika, stb.) - Külső kulcs lesz
        public int CompanyTypeId { get; set; }

        public CompanyType? CompanyType { get; set; }

        // Logikai törlés (ha true, akkor töröltnek tekintjük, de az adatbázisban marad)
        public bool IsDeleted { get; set; } = false; // Alapértelmezetten hamis

        // Mikor jött létre a cég a rendszerben
        public DateTime CreationDate { get; set; } = DateTime.UtcNow; // Azonnali értékadás

        // Cég neve (nvarchar 200 volt a modellben)
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        // Cím adatok
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

        // Cégadatok (Szlovák specifikus mezők a modelled alapján)
        [MaxLength(8)]
        public string ICO { get; set; } = string.Empty;

        [MaxLength(10)]
        public string DIC { get; set; } = string.Empty;

        [MaxLength(13)]
        public string ICDPH { get; set; } = string.Empty;

        // Elfogadta-e a szerződést
        public bool AcceptedCompanyUserAgreement { get; set; }
    }
}