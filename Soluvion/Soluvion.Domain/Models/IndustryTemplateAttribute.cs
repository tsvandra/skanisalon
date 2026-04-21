using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Soluvion.Domain.Models
{
    public class IndustryTemplateAttribute
    {
        public int Id { get; set; }

        public int CompanyTypeId { get; set; }
        public CompanyType? CompanyType { get; set; }

        [Required]
        [MaxLength(100)]
        public string Key { get; set; } = string.Empty; // pl. "hair_length"

        [Required]
        [MaxLength(100)]
        public string Label { get; set; } = string.Empty; // pl. "Hajhossz"

        [Required]
        [MaxLength(50)]
        public string DataType { get; set; } = "text"; // Lehet: "text", "select", "number", "boolean"

        [Column(TypeName = "jsonb")]
        public List<string> Options { get; set; } = new();

        public bool IsRequired { get; set; } = false; // Kötelező-e kitölteni
        public bool ShowOnPublicBooking { get; set; } = false; // Megjelenjen-e a vendégnek az online foglaláskor
        public bool IsActive { get; set; } = true;
    }
}