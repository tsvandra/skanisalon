using Soluvion.API.Models;

namespace Soluvion.API.Services
{
    public interface IAuthService
    {
        Task<User?> RegisterAsync(string username, string password);
        Task<string?> LoginAsync(string username, string password);
    }
}