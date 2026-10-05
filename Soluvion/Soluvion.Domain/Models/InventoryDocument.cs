using Soluvion.Domain.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Soluvion.Domain.Models
{
    public class InventoryDocument
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }

        public InventoryDocumentType Type { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Note { get; set; }

        // Navigation
        public Company? Company { get; set; } public int? AppointmentId { get; set; } public Appointment? Appointment { get; set; }

        public ICollection<InventoryDocumentItem> Items { get; set; } = new List<InventoryDocumentItem>();

        /// <summary>Igaz, ha a bizonylatot sztornózták (pl. a napi zárás visszavonásakor).</summary>
        public bool IsReversed { get; set; } = false;

        /// <summary>Sztornó bizonylatnál: az eredeti (sztornózott) bizonylat azonosítója.</summary>
        public int? ReversalOfDocumentId { get; set; }
    }
}

