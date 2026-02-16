using Soluvion.API.Models;

namespace Soluvion.API.Services
{
    public interface ITenantContext
    {
        Company? CurrentCompany { get; set; }
    }
}