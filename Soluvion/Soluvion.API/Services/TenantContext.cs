using Soluvion.API.Models;

namespace Soluvion.API.Services
{
    public class TenantContext : ITenantContext
    {
        public Company? CurrentCompany { get; set; }
    }
}