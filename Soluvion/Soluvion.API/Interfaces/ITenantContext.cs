using Soluvion.Domain.Models;

namespace Soluvion.API.Interfaces
{
    public interface ITenantContext
    {
        Company? CurrentCompany { get; set; }
    }
}