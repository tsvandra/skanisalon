using Soluvion.Domain.Models.Enums;

namespace Soluvion.Domain.Models
{
    public class CompanyEmployee
    {
        public int Id { get; set; }

        public int CompanyId { get; set; }
        public Company? Company { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public EmployeeRole Role { get; set; }
        public bool IsActive { get; set; } = true;
    }
}