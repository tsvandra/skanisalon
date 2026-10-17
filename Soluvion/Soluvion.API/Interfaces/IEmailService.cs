using Soluvion.Domain.Models;

namespace Soluvion.API.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendTransactionalEmailAsync(
            Company company,
            string toEmail,
            string toName,
            string subject,
            string htmlBody);
    }
}
