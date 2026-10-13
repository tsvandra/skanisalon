using Soluvion.Domain.Models;

namespace Soluvion.API.Interfaces
{
    public interface IUserContext
    {
        bool IsAuthenticated { get; }
        int? UserId { get; }
        string? Username { get; }
        string? Role { get; }
        int? CompanyId { get; }
        Task<CompanyEmployee?> GetCurrentEmployeeAsync();
    }
}

