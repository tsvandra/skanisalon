using System.ComponentModel.DataAnnotations;

namespace Soluvion.Domain.Models
{
    public class CompanyType
    {
        public int Id { get; set; }

        [MaxLength(50)] 
        public string Name { get; set; } = string.Empty;
        public ICollection<IndustryTemplateAttribute> TemplateAttributes { get; set; } = new List<IndustryTemplateAttribute>();
    }
}