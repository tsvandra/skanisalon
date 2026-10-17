using System.Text;
using System.Text.Json;
using Soluvion.API.Interfaces;
using Soluvion.Domain.Models;

namespace Soluvion.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<bool> SendTransactionalEmailAsync(
            Company company,
            string toEmail,
            string toName,
            string subject,
            string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                return false;

            var provider = (_configuration["Email:Provider"] ?? "Brevo").Trim();
            var apiKey = _configuration["Email:ApiKey"];
            var fromEmail = _configuration["Email:FromAddress"] ?? company.Email ?? "noreply@beemagic.sk";
            var fromName = !string.IsNullOrWhiteSpace(company.Name) ? company.Name : "BeeMagic Salon";
            var replyToEmail = !string.IsNullOrWhiteSpace(company.Email) ? company.Email : fromEmail;

            if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains("IDE_JON"))
            {
                _logger.LogInformation(
                    "[EmailService] Nincs beállítva Email:ApiKey, az e-mail küldés kihagyva (Címzett: {ToEmail}, Tárgy: {Subject}).",
                    toEmail, subject);
                return false;
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                if (string.Equals(provider, "Resend", StringComparison.OrdinalIgnoreCase))
                {
                    var payload = new
                    {
                        from = $"{fromName} <{fromEmail}>",
                        to = new[] { toEmail },
                        reply_to = replyToEmail,
                        subject,
                        html = htmlBody
                    };

                    using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
                    req.Headers.Add("Authorization", $"Bearer {apiKey}");
                    req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                    var resp = await client.SendAsync(req);
                    if (!resp.IsSuccessStatusCode)
                    {
                        var err = await resp.Content.ReadAsStringAsync();
                        _logger.LogWarning("[EmailService - Resend] Sikertelen küldés ({Status}): {Error}", resp.StatusCode, err);
                        return false;
                    }
                    return true;
                }
                else
                {
                    // Alapértelmezett: Brevo (Sendinblue) HTTP v3 API (Railway 443-as port kompatibilis)
                    var payload = new
                    {
                        sender = new { name = fromName, email = fromEmail },
                        to = new[] { new { email = toEmail, name = string.IsNullOrWhiteSpace(toName) ? toEmail : toName } },
                        replyTo = new { email = replyToEmail, name = fromName },
                        subject,
                        htmlContent = htmlBody
                    };

                    using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
                    req.Headers.Add("api-key", apiKey);
                    req.Headers.Add("accept", "application/json");
                    req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                    var resp = await client.SendAsync(req);
                    if (!resp.IsSuccessStatusCode)
                    {
                        var err = await resp.Content.ReadAsStringAsync();
                        _logger.LogWarning("[EmailService - Brevo] Sikertelen küldés ({Status}): {Error}", resp.StatusCode, err);
                        return false;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EmailService] Hiba történt az e-mail küldése közben.");
                return false;
            }
        }
    }
}
