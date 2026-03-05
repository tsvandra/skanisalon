using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Soluvion.API.Models.DTOs;
using Soluvion.API.Services;

namespace Soluvion.API.Controllers
{
    // A Route direkt maradt "service", hogy a Frontend egyelőre ne törjön el! 
    // Ha a frontenden is átírod, cseréld le "api/[controller]"-re.
    [Route("api/service")]
    [ApiController]
    public class CatalogController : ControllerBase
    {
        private readonly ICatalogService _catalogService;

        public CatalogController(ICatalogService catalogService)
        {
            _catalogService = catalogService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CatalogDto>>> GetCatalog()
        {
            var dtos = await _catalogService.GetCatalogAsync();
            return Ok(dtos);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<CatalogDto>> PostCatalogItem([FromBody] CatalogDto dto)
        {
            var resultDto = await _catalogService.CreateCatalogItemAsync(dto);
            if (resultDto == null) return Unauthorized();

            return CreatedAtAction(nameof(GetCatalog), new { id = resultDto.Id }, resultDto);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> PutCatalogItem(int id, [FromBody] CatalogDto dto)
        {
            if (id != dto.Id) return BadRequest("Az ID nem egyezik.");

            var updatedDto = await _catalogService.UpdateCatalogItemAsync(id, dto);
            if (updatedDto == null) return NotFound();

            return Ok(updatedDto);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteCatalogItem(int id)
        {
            var success = await _catalogService.DeleteCatalogItemAsync(id);
            if (!success) return NotFound();

            return NoContent();
        }
    }
}