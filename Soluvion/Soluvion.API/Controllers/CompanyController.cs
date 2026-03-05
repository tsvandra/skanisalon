using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Soluvion.API.DTOs;
using Soluvion.API.Models.DTOs;
using Soluvion.API.Services;

namespace Soluvion.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompanyController : ControllerBase
    {
        private readonly ICompanyService _companyService;

        public CompanyController(ICompanyService companyService)
        {
            _companyService = companyService;
        }

        [HttpGet("public-config")]
        [AllowAnonymous]
        public async Task<ActionResult<CompanyPublicProfileDto>> GetPublicConfig()
        {
            var dto = await _companyService.GetPublicConfigAsync();
            if (dto == null) return NotFound("A kért szalon nem található.");
            return Ok(dto);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<CompanySettingsDto>> GetCompanySettings(int id)
        {
            var companyClaim = User.FindFirst("CompanyId");
            if (companyClaim == null || int.Parse(companyClaim.Value) != id)
                return Forbid();

            var dto = await _companyService.GetCompanySettingsAsync(id);
            if (dto == null) return NotFound();

            return Ok(dto);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateCompany(int id, [FromBody] CompanySettingsDto dto)
        {
            var companyClaim = User.FindFirst("CompanyId");
            if (companyClaim == null) return Unauthorized();
            if (id != int.Parse(companyClaim.Value)) return Forbid();

            var updatedDto = await _companyService.UpdateCompanyAsync(id, dto);
            if (updatedDto == null) return NotFound();

            return Ok(updatedDto);
        }

        // --- KÉPFELTÖLTÉS (Már tisztán, Service-en keresztül) ---
        [HttpPost("upload/logo")]
        [Authorize]
        public async Task<IActionResult> UploadLogo(IFormFile file) => await UploadBrandingImage(file, "logo");

        [HttpPost("upload/hero")]
        [Authorize]
        public async Task<IActionResult> UploadHero(IFormFile file) => await UploadBrandingImage(file, "hero");

        [HttpPost("upload/footer")]
        [Authorize]
        public async Task<IActionResult> UploadFooter(IFormFile file) => await UploadBrandingImage(file, "footer");

        private async Task<IActionResult> UploadBrandingImage(IFormFile file, string type)
        {
            if (file == null || file.Length == 0) return BadRequest("Nincs fájl.");

            var companyClaim = User.FindFirst("CompanyId");
            if (companyClaim == null) return Unauthorized();
            int companyId = int.Parse(companyClaim.Value);

            var resultUrl = await _companyService.UploadBrandingImageAsync(companyId, file, type);
            if (resultUrl == null) return BadRequest("Hiba történt a kép feltöltése vagy mentése során.");

            return Ok(new { url = resultUrl });
        }
    }
}