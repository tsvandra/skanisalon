using System.ComponentModel.DataAnnotations;

namespace Soluvion.API.Models
{
    public class CompanyType
    {
        public int Id { get; set; }

        [MaxLength(50)] // Kicsit nagyobbat adtam neki, mint a modellben (20), a biztonság kedvéért
        public string Name { get; set; } = string.Empty;
    }
}