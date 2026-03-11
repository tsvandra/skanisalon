using Soluvion.Domain.Models;

namespace Soluvion.API.Interfaces
{
    public interface IAuthService
    {
        Task<User?> RegisterAsync(string username, string password);
        Task<string?> LoginAsync(string username, string password);
    }
}