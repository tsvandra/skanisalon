using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Soluvion.API.DTOs;
using Soluvion.API.Interfaces;

namespace Soluvion.API.Controllers
{
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
            if (id != dto.Id) return BadRequest(new { errorCode = "BAD_REQUEST" });

            var updatedDto = await _catalogService.UpdateCatalogItemAsync(id, dto);
            if (updatedDto == null) return NotFound(new { errorCode = "NOT_FOUND" });

            return Ok(updatedDto);
        }

        [HttpPut("variant/{variantId}/recipe")]
        [Authorize]
        public async Task<IActionResult> UpdateVariantRecipe(int variantId, [FromBody] List<ServiceVariantProductDto> products)
        {
            var success = await _catalogService.UpdateVariantRecipeAsync(variantId, products);
            if (!success) return NotFound(new { errorCode = "NOT_FOUND" });
            return Ok();
        }

        [HttpPost("generate-pictogram")]
        [Authorize]
        public async Task<IActionResult> GeneratePictogram([FromBody] GeneratePictogramRequestDto dto)
        {
            var svg = await _catalogService.GeneratePictogramSvgAsync(dto);
            return Ok(new { svg });
        }

        [HttpPost("generate-missing-pictograms")]
        [Authorize]
        public async Task<IActionResult> GenerateMissingPictograms([FromBody] GenerateMissingPictogramsRequestDto? dto)
        {
            var count = await _catalogService.GenerateMissingPictogramsAsync(dto?.LanguageCode);
            return Ok(new { generatedCount = count });
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteCatalogItem(int id)
        {
            var success = await _catalogService.DeleteCatalogItemAsync(id);
            if (!success) return NotFound(new { errorCode = "NOT_FOUND" });

            return NoContent();
        }
    }
}

