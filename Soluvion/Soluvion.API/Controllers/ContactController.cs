using Microsoft.AspNetCore.Mvc;
using Soluvion.API.DTOs;
using Soluvion.API.Interfaces;

namespace Soluvion.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactController : ControllerBase
    {
        private readonly ITenantContext _tenantContext;
        private readonly IEmailService _emailService;

        public ContactController(ITenantContext tenantContext, IEmailService emailService)
        {
            _tenantContext = tenantContext;
            _emailService = emailService;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ContactMessageDto dto)
        {
            var company = _tenantContext.CurrentCompany;

            if (company == null)
                return BadRequest(new { errorCode = "COMPANY_NOT_FOUND" });

            if (!string.IsNullOrWhiteSpace(company.Email))
            {
                var subject = $"[Kapcsolat - {company.Name}] Új üzenet: {dto.Name}";
                var html = $@"
                    <div style='font-family: sans-serif; line-height: 1.6; color: #1f2937;'>
                        <h3>Új üzenet érkezett a weboldal Kapcsolat űrlapjáról</h3>
                        <p><strong>Feladó neve:</strong> {System.Net.WebUtility.HtmlEncode(dto.Name)}</p>
                        <p><strong>Feladó e-mail címe:</strong> <a href='mailto:{System.Net.WebUtility.HtmlEncode(dto.Email)}'>{System.Net.WebUtility.HtmlEncode(dto.Email)}</a></p>
                        <hr style='border: none; border-top: 1px solid #e5e7eb; margin: 16px 0;' />
                        <p style='white-space: pre-wrap;'>{System.Net.WebUtility.HtmlEncode(dto.Message)}</p>
                    </div>";

                await _emailService.SendTransactionalEmailAsync(
                    company,
                    company.Email,
                    company.Name ?? "Salon",
                    subject,
                    html);
            }

            return Ok(new { success = true });
        }
    }
}