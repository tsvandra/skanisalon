using Soluvion.API.Interfaces;
using Soluvion.Domain.Models;

namespace Soluvion.API.Services
{
    public class TenantContext : ITenantContext
    {
        public Company? CurrentCompany { get; set; }
    }
}