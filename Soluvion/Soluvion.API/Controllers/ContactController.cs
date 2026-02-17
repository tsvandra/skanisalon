using Microsoft.AspNetCore.Mvc;
using Soluvion.API.Models.DTOs;
using Soluvion.API.Services;

namespace Soluvion.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactController : ControllerBase
    {
        private readonly ITenantContext _tenantContext;

        public ContactController(ITenantContext tenantContext)
        {
            _tenantContext = tenantContext;
        }

        [HttpPost]
        public IActionResult SendMessage([FromBody] ContactMessageDto dto)
        {
            // 1. Megnézzük, melyik cégnél vagyunk
            var company = _tenantContext.CurrentCompany;

            if (company == null)
                return BadRequest("Nem sikerült azonosítani a címzett szalont.");

            // 2. EMAIL KÜLDÉS SZIMULÁCIÓ
            // Itt kellene meghívni egy IEmailService-t (pl. MailKit vagy SendGrid).
            // Most csak kiírjuk a konzolra, hogy lássuk a működést.
            Console.WriteLine($"[EMAIL KÜLDÉS] Címzett: {company.Email} | Feladó: {dto.Email} ({dto.Name}) | Üzenet: {dto.Message}");

            // 3. Válasz a frontendnek
            return Ok(new { message = "Az üzenetet sikeresen továbbítottuk!" });
        }
    }
}